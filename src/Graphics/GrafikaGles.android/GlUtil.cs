using System;
using Android.Opengl;
using Android.Util;
using Java.Nio;

namespace Grafika.GLES
{
	public class GlUtil
	{
		public const string TAG = "Grafika";

		/** Identity matrix for general use.  Don't modify or life will get weird. */
		public static float[] IDENTITY_MATRIX
		{
			get
			{
				var mat = new float[16];
				Matrix.SetIdentityM(mat, 0);
				return mat;
			}
		}

        //static {
        //    IDENTITY_MATRIX = new float[16];
        //    Matrix.setIdentityM(IDENTITY_MATRIX, 0);
        //}

        private const int SIZEOF_FLOAT = 4;

        //private GlUtil() { }     // do not instantiate

        /**
         * Creates a new program from the supplied vertex and fragment shaders.
         *
         * @return A handle to the program, or 0 on failure.
         */
        public static int createProgram(string vertexSource, string fragmentSource)
        {
            int vertexShader = loadShader(GLES20.GlVertexShader, vertexSource);
            if (vertexShader == 0)
            {
                return 0;
            }
            int pixelShader = loadShader(GLES20.GlFragmentShader, fragmentSource);
            if (pixelShader == 0)
            {
                return 0;
            }

            int program = GLES20.GlCreateProgram();
            checkGlError("glCreateProgram");
            if (program == 0)
            {
                Log.Error(TAG, "Could not create program");
            }
            GLES20.GlAttachShader(program, vertexShader);
            checkGlError("glAttachShader");
            GLES20.GlAttachShader(program, pixelShader);
            checkGlError("glAttachShader");
            GLES20.GlLinkProgram(program);
            int[] linkStatus = new int[1];
            GLES20.GlGetProgramiv(program, GLES20.GlLinkStatus, linkStatus, 0);
            if (linkStatus[0] != GLES20.GlTrue)
            {
                Log.Error(TAG, "Could not link program: ");
                Log.Error(TAG, GLES20.GlGetProgramInfoLog(program));
                GLES20.GlDeleteProgram(program);
                program = 0;
            }
            return program;
        }

        /**
         * Compiles the provided shader source.
         *
         * @return A handle to the shader, or 0 on failure.
         */
        public static int loadShader(int shaderType, string source)
        {
            int shader = GLES20.GlCreateShader(shaderType);
            checkGlError("glCreateShader type=" + shaderType);
            GLES20.GlShaderSource(shader, source);
            GLES20.GlCompileShader(shader);
            int[] compiled = new int[1];
            GLES20.GlGetShaderiv(shader, GLES20.GlCompileStatus, compiled, 0);
            if (compiled[0] == 0)
            {
                Log.Error(TAG, "Could not compile shader " + shaderType + ":");
                Log.Error(TAG, " " + GLES20.GlGetShaderInfoLog(shader));
                GLES20.GlDeleteShader(shader);
                shader = 0;
            }
            return shader;
        }

        /**
         * Checks to see if a GLES error has been raised.
         */
        public static void checkGlError(string op)
        {
            int error = GLES20.GlGetError();
            if (error != GLES20.GlNoError)
            {
                string msg = op + ": glError 0x" + error.ToString();
                Log.Error(TAG, msg);
                throw new Exception(msg);
            }
        }

        /**
         * Checks to see if the location we obtained is valid.  GLES returns -1 if a label
         * could not be found, but does not set the GL error.
         * <p>
         * Throws a RuntimeException if the location is invalid.
         */
        public static void checkLocation(int location, string label)
        {
            if (location < 0)
            {
                throw new Exception("Unable to locate '" + label + "' in program");
            }
        }

        /**
         * Creates a texture from raw data.
         *
         * @param data Image data, in a "direct" ByteBuffer.
         * @param width Texture width, in pixels (not bytes).
         * @param height Texture height, in pixels.
         * @param format Image data format (use constant appropriate for glTexImage2D(), e.g. GL_RGBA).
         * @return Handle to texture.
         */
        public static int createImageTexture(ByteBuffer data, int width, int height, int format)
        {
            int[] textureHandles = new int[1];
            int textureHandle;

            GLES20.GlGenTextures(1, textureHandles, 0);
            textureHandle = textureHandles[0];
            GlUtil.checkGlError("glGenTextures");

            // Bind the texture handle to the 2D texture target.
            GLES20.GlBindTexture(GLES20.GlTexture2d, textureHandle);

            // Configure min/mag filtering, i.e. what scaling method do we use if what we're rendering
            // is smaller or larger than the source image.
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMinFilter,
                    GLES20.GlLinear);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMagFilter,
                    GLES20.GlLinear);
            GlUtil.checkGlError("loadImageTexture");

            // Load the data from the buffer into the texture handle.
            GLES20.GlTexImage2D(GLES20.GlTexture2d, /*level*/ 0, format,
                    width, height, /*border*/ 0, format, GLES20.GlUnsignedByte, data);
            GlUtil.checkGlError("loadImageTexture");

            return textureHandle;
        }

        /**
         * Allocates a direct float buffer, and populates it with the float array data.
         */
        public static FloatBuffer createFloatBuffer(float[] coords)
        {
            // Allocate a direct ByteBuffer, using 4 bytes per float, and copy coords into it.
            ByteBuffer bb = ByteBuffer.AllocateDirect(coords.Length * SIZEOF_FLOAT);
            bb.Order(ByteOrder.NativeOrder());
            FloatBuffer fb = bb.AsFloatBuffer();
            fb.Put(coords);
            fb.Position(0);
            return fb;
        }

        /**
         * Writes GL version info to the log.
         */
        public static void logVersionInfo()
        {
            Log.Info(TAG, "vendor  : " + GLES20.GlGetString(GLES20.GlVendor));
            Log.Info(TAG, "renderer: " + GLES20.GlGetString(GLES20.GlRenderer));
            Log.Info(TAG, "version : " + GLES20.GlGetString(GLES20.GlVersion));

            if (false)
            {
                int[] values = new int[1];
                GLES30.GlGetIntegerv(GLES30.GlMajorVersion, values, 0);
                int majorVersion = values[0];
                GLES30.GlGetIntegerv(GLES30.GlMinorVersion, values, 0);
                int minorVersion = values[0];
                if (GLES30.GlGetError() == GLES30.GlNoError)
                {
                    Log.Info(TAG, "iversion: " + majorVersion + "." + minorVersion);
                }
            }
        }
    }
}
