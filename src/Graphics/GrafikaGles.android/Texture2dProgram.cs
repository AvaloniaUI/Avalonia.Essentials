using System;
using Android.Opengl;
using Android.Util;
using Java.Nio;

namespace Grafika.GLES
{
    public class Texture2dProgram
    {
        private static string TAG = GlUtil.TAG;

        public enum ProgramType
        {
            TEXTURE_2D, TEXTURE_EXT, TEXTURE_EXT_BW, TEXTURE_EXT_FILT
        }

        // Simple vertex shader, used for all programs.
        private static string VERTEX_SHADER =
            "uniform mat4 uMVPMatrix;\n" +
            "uniform mat4 uTexMatrix;\n" +
            "attribute vec4 aPosition;\n" +
            "attribute vec4 aTextureCoord;\n" +
            "varying vec2 vTextureCoord;\n" +
            "void main() {\n" +
            "    gl_Position = uMVPMatrix * aPosition;\n" +
            "    vTextureCoord = (uTexMatrix * aTextureCoord).xy;\n" +
            "}\n";

        // Simple fragment shader for use with external 2D textures (e.g. what we get from
        // SurfaceTexture).
        private static string FRAGMENT_SHADER_EXT =
            "#extension GL_OES_EGL_image_external : require\n" +
            "precision mediump float;\n" +
            "varying vec2 vTextureCoord;\n" +
            "uniform samplerExternalOES sTexture;\n" +
            "void main() {\n" +
            "    gl_FragColor = texture2D(sTexture, vTextureCoord);\n" +
            "}\n";

        private ProgramType mProgramType;

        // Handles to the GL program and various components of it.
        private int mProgramHandle;
        private int muMVPMatrixLoc;
        private int muTexMatrixLoc;
        private int muKernelLoc;
        private int muTexOffsetLoc;
        private int muColorAdjustLoc;
        private int maPositionLoc;
        private int maTextureCoordLoc;

        private int mTextureTarget;

        /**
         * Prepares the program in the current EGL context.
         */
        public Texture2dProgram(ProgramType programType)
        {
            mProgramType = programType;

            switch (programType)
            {
                case ProgramType.TEXTURE_EXT:
                    mTextureTarget = GLES11Ext.GlTextureExternalOes;
                    mProgramHandle = GlUtil.createProgram(VERTEX_SHADER, FRAGMENT_SHADER_EXT);
                    break;
                default:
                    throw new Exception("Unhandled type " + programType);
            }
            if (mProgramHandle == 0)
            {
                throw new Exception("Unable to create program");
            }
            Log.Debug(TAG, "Created program " + mProgramHandle + " (" + programType + ")");

            // get locations of attributes and uniforms

            maPositionLoc = GLES20.GlGetAttribLocation(mProgramHandle, "aPosition");
            GlUtil.checkLocation(maPositionLoc, "aPosition");
            maTextureCoordLoc = GLES20.GlGetAttribLocation(mProgramHandle, "aTextureCoord");
            GlUtil.checkLocation(maTextureCoordLoc, "aTextureCoord");
            muMVPMatrixLoc = GLES20.GlGetUniformLocation(mProgramHandle, "uMVPMatrix");
            GlUtil.checkLocation(muMVPMatrixLoc, "uMVPMatrix");
            muTexMatrixLoc = GLES20.GlGetUniformLocation(mProgramHandle, "uTexMatrix");
            GlUtil.checkLocation(muTexMatrixLoc, "uTexMatrix");
            muKernelLoc = GLES20.GlGetUniformLocation(mProgramHandle, "uKernel");
            if (muKernelLoc < 0)
            {
                // no kernel in this one
                muKernelLoc = -1;
                muTexOffsetLoc = -1;
                muColorAdjustLoc = -1;
            }
        }

        /**
         * Releases the program.
         * <p>
         * The appropriate EGL context must be current (i.e. the one that was used to create
         * the program).
         */
        public void release()
        {
            Log.Debug(TAG, "deleting program " + mProgramHandle);
            GLES20.GlDeleteProgram(mProgramHandle);
            mProgramHandle = -1;
        }

        /**
         * Returns the program type.
         */
        public ProgramType getProgramType()
        {
            return mProgramType;
        }

        /**
         * Creates a texture object suitable for use with this program.
         * <p>
         * On exit, the texture will be bound.
         */
        public int createTextureObject()
        {
            int[] textures = new int[1];
            GLES20.GlGenTextures(1, textures, 0);
            GlUtil.checkGlError("glGenTextures");

            int texId = textures[0];
            GLES20.GlBindTexture(mTextureTarget, texId);
            GlUtil.checkGlError("glBindTexture " + texId);

            GLES20.GlTexParameterf(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureMinFilter,
                    GLES20.GlNearest);
            GLES20.GlTexParameterf(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureMagFilter,
                    GLES20.GlLinear);
            GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureWrapS,
                    GLES20.GlClampToEdge);
            GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureWrapT,
                    GLES20.GlClampToEdge);
            GlUtil.checkGlError("glTexParameter");

            return texId;
        }

        /**
         * Issues the draw call.  Does the full setup on every call.
         *
         * @param mvpMatrix The 4x4 projection matrix.
         * @param vertexBuffer Buffer with vertex position data.
         * @param firstVertex Index of first vertex to use in vertexBuffer.
         * @param vertexCount Number of vertices in vertexBuffer.
         * @param coordsPerVertex The number of coordinates per vertex (e.g. x,y is 2).
         * @param vertexStride Width, in bytes, of the position data for each vertex (often
         *        vertexCount * sizeof(float)).
         * @param texMatrix A 4x4 transformation matrix for texture coords.  (Primarily intended
         *        for use with SurfaceTexture.)
         * @param texBuffer Buffer with vertex texture data.
         * @param texStride Width, in bytes, of the texture data for each vertex.
         */
        public void draw(float[] mvpMatrix, FloatBuffer vertexBuffer, int firstVertex,
                int vertexCount, int coordsPerVertex, int vertexStride,
                float[] texMatrix, FloatBuffer texBuffer, int textureId, int texStride)
        {
            GlUtil.checkGlError("draw start");

            // Select the program.
            GLES20.GlUseProgram(mProgramHandle);
            GlUtil.checkGlError("glUseProgram");

            // Set the texture.
            GLES20.GlActiveTexture(GLES20.GlTexture0);
            GLES20.GlBindTexture(mTextureTarget, textureId);

            // Copy the model / view / projection matrix over.
            GLES20.GlUniformMatrix4fv(muMVPMatrixLoc, 1, false, mvpMatrix, 0);
            GlUtil.checkGlError("glUniformMatrix4fv");

            // Copy the texture transformation matrix over.
            GLES20.GlUniformMatrix4fv(muTexMatrixLoc, 1, false, texMatrix, 0);
            GlUtil.checkGlError("glUniformMatrix4fv");

            // Enable the "aPosition" vertex attribute.
            GLES20.GlEnableVertexAttribArray(maPositionLoc);
            GlUtil.checkGlError("glEnableVertexAttribArray");

            // Connect vertexBuffer to "aPosition".
            GLES20.GlVertexAttribPointer(maPositionLoc, coordsPerVertex,
                GLES20.GlFloat, false, vertexStride, vertexBuffer);
            GlUtil.checkGlError("glVertexAttribPointer");

            // Enable the "aTextureCoord" vertex attribute.
            GLES20.GlEnableVertexAttribArray(maTextureCoordLoc);
            GlUtil.checkGlError("glEnableVertexAttribArray");

            // Connect texBuffer to "aTextureCoord".
            GLES20.GlVertexAttribPointer(maTextureCoordLoc, 2,
                    GLES20.GlFloat, false, texStride, texBuffer);
            GlUtil.checkGlError("glVertexAttribPointer");

            // Draw the rect.
            GLES20.GlDrawArrays(GLES20.GlTriangleStrip, firstVertex, vertexCount);
            GlUtil.checkGlError("glDrawArrays");

            // Done -- disable vertex array, texture, and program.
            GLES20.GlDisableVertexAttribArray(maPositionLoc);
            GLES20.GlDisableVertexAttribArray(maTextureCoordLoc);
            GLES20.GlBindTexture(mTextureTarget, 0);
            GLES20.GlUseProgram(0);
        }
    }
}
