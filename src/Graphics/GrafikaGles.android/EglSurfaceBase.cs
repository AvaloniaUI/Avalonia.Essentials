using System;
using Android.Graphics;
using Android.Opengl;
using Android.Util;
using Java.IO;
using Java.Nio;

namespace Grafika.GLES
{
    public class EglSurfaceBase
    {
        protected const string TAG = GlUtil.TAG;

        // EglCore object we're associated with.  It may be associated with multiple surfaces.
        protected EglCore mEglCore;

        private EGLSurface mEGLSurface = EGL14.EglNoSurface;
        private int mWidth = -1;
        private int mHeight = -1;

        protected EglSurfaceBase(EglCore eglCore)
        {
            mEglCore = eglCore;
        }

        /**
         * Creates a window surface.
         * <p>
         * @param surface May be a Surface or SurfaceTexture.
         */
        public void createWindowSurface(Java.Lang.Object surface)
        {
            if (mEGLSurface != EGL14.EglNoSurface)
            {
                throw new Exception("surface already created");
            }
            mEGLSurface = mEglCore.createWindowSurface(surface);

            // Don't cache width/height here, because the size of the underlying surface can change
            // out from under us (see e.g. HardwareScalerActivity).
            //mWidth = mEglCore.querySurface(mEGLSurface, EGL14.EGL_WIDTH);
            //mHeight = mEglCore.querySurface(mEGLSurface, EGL14.EGL_HEIGHT);
        }

        /**
         * Creates an off-screen surface.
         */
        public void createOffscreenSurface(int width, int height)
        {
            if (mEGLSurface != EGL14.EglNoSurface)
            {
                throw new Exception("surface already created");
            }
            mEGLSurface = mEglCore.createOffscreenSurface(width, height);
            mWidth = width;
            mHeight = height;
        }

        /**
         * Returns the surface's width, in pixels.
         * <p>
         * If this is called on a window surface, and the underlying surface is in the process
         * of changing size, we may not see the new size right away (e.g. in the "surfaceChanged"
         * callback).  The size should match after the next buffer swap.
         */
        public int getWidth()
        {
            if (mWidth < 0)
            {
                return mEglCore.querySurface(mEGLSurface, EGL14.EglWidth);
            }
            else
            {
                return mWidth;
            }
        }

        /**
         * Returns the surface's height, in pixels.
         */
        public int getHeight()
        {
            if (mHeight < 0)
            {
                return mEglCore.querySurface(mEGLSurface, EGL14.EglHeight);
            }
            else
            {
                return mHeight;
            }
        }

        /**
         * Release the EGL surface.
         */
        public void releaseEglSurface()
        {
            mEglCore.releaseSurface(mEGLSurface);
            mEGLSurface = EGL14.EglNoSurface;
            mWidth = mHeight = -1;
        }

        /**
         * Makes our EGL context and surface current.
         */
        public void makeCurrent()
        {
            mEglCore.makeCurrent(mEGLSurface);
        }

        /**
         * Makes our EGL context and surface current for drawing, using the supplied surface
         * for reading.
         */
        public void makeCurrentReadFrom(EglSurfaceBase readSurface)
        {
            mEglCore.makeCurrent(mEGLSurface, readSurface.mEGLSurface);
        }

        /**
         * Calls eglSwapBuffers.  Use this to "publish" the current frame.
         *
         * @return false on failure
         */
        public bool swapBuffers()
        {
            bool result = mEglCore.swapBuffers(mEGLSurface);
            if (!result)
            {
                Log.Debug(TAG, "WARNING: swapBuffers() failed");
            }
            return result;
        }

        /**
         * Sends the presentation time stamp to EGL.
         *
         * @param nsecs Timestamp, in nanoseconds.
         */
        public void setPresentationTime(long nsecs)
        {
            mEglCore.setPresentationTime(mEGLSurface, nsecs);
        }

        public Bitmap getBitmap()
        {
            if (!mEglCore.isCurrent(mEGLSurface))
            {
                throw new Exception("Expected EGL context/surface is not current");
            }

            // glReadPixels fills in a "direct" ByteBuffer with what is essentially big-endian RGBA
            // data (i.e. a byte of red, followed by a byte of green...).  While the Bitmap
            // constructor that takes an int[] wants little-endian ARGB (blue/red swapped), the
            // Bitmap "copy pixels" method wants the same format GL provides.
            //
            // Ideally we'd have some way to re-use the ByteBuffer, especially if we're calling
            // here often.
            //
            // Making this even more interesting is the upside-down nature of GL, which means
            // our output will look upside down relative to what appears on screen if the
            // typical GL conventions are used.

            int width = getWidth();
            int height = getHeight();

            ByteBuffer buf = ByteBuffer.AllocateDirect(width * height * 4);
            buf.Order(ByteOrder.LittleEndian);
            GLES20.GlReadPixels(0, 0, width, height,
                    GLES20.GlRgba, GLES20.GlUnsignedByte, buf);
            GlUtil.checkGlError("glReadPixels");
            buf.Rewind();

            Bitmap bmp = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
            bmp.CopyPixelsFromBuffer(buf);

            return bmp;
        }
    }
}
