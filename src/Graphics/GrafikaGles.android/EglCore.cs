using System;
using Android.Graphics;
using Android.Opengl;
using Android.Util;
using Android.Views;

namespace Grafika.GLES
{
    public class EglCore : Java.Lang.Object
    {
        private const string TAG = GlUtil.TAG;

        /**
         * Constructor flag: surface must be recordable.  This discourages EGL from using a
         * pixel format that cannot be converted efficiently to something usable by the video
         * encoder.
         */
        public const int FLAG_RECORDABLE = 0x01;

        /**
         * Constructor flag: ask for GLES3, fall back to GLES2 if not available.  Without this
         * flag, GLES2 is used.
         */
        public const int FLAG_TRY_GLES3 = 0x02;

        // Android-specific extension.
        private const int EGL_RECORDABLE_ANDROID = 0x3142;

        private EGLDisplay mEGLDisplay = EGL14.EglNoDisplay;
        private EGLContext mEGLContext = EGL14.EglNoContext;
        private EGLConfig mEGLConfig = null;
        private int mGlVersion = -1;

        /**
         * Prepares EGL display and context.
         * <p>
         * @param sharedContext The context to share, or null if sharing is not desired.
         * @param flags Configuration bit flags, e.g. FLAG_RECORDABLE.
         */
        public EglCore(EGLContext sharedContext = null, int flags = 0)
        {
            if (mEGLDisplay != EGL14.EglNoDisplay)
            {
                throw new Exception("EGL already set up");
            }

            if (sharedContext == null)
            {
                sharedContext = EGL14.EglNoContext;
            }

            mEGLDisplay = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay);
            if (mEGLDisplay == EGL14.EglNoDisplay)
            {
                throw new Exception("unable to get EGL14 display");
            }
            int[] version = new int[2];
            if (!EGL14.EglInitialize(mEGLDisplay, version, 0, version, 1))
            {
                mEGLDisplay = null;
                throw new Exception("unable to initialize EGL14");
            }

            // Try to get a GLES3 context, if requested.
            if ((flags & FLAG_TRY_GLES3) != 0)
            {
                //Log.d(TAG, "Trying GLES 3");
                EGLConfig config = getConfig(flags, 3);
                if (config != null)
                {
                    int[] attrib3_list = {
                        EGL14.EglContextClientVersion, 3,
                        EGL14.EglNone
                };
                    EGLContext context = EGL14.EglCreateContext(mEGLDisplay, config, sharedContext,
                            attrib3_list, 0);

                    if (EGL14.EglGetError() == EGL14.EglSuccess)
                    {
                        //Log.d(TAG, "Got GLES 3 config");
                        mEGLConfig = config;
                        mEGLContext = context;
                        mGlVersion = 3;
                    }
                }
            }
            if (mEGLContext == EGL14.EglNoContext)
            {  // GLES 2 only, or GLES 3 attempt failed
               //Log.d(TAG, "Trying GLES 2");
                EGLConfig config = getConfig(flags, 2);
                if (config == null)
                {
                    throw new Exception("Unable to find a suitable EGLConfig");
                }
                int[] attrib2_list = {
                    EGL14.EglContextClientVersion, 2,
                    EGL14.EglNone
            };
                EGLContext context = EGL14.EglCreateContext(mEGLDisplay, config, sharedContext,
                        attrib2_list, 0);
                checkEglError("eglCreateContext");
                mEGLConfig = config;
                mEGLContext = context;
                mGlVersion = 2;
            }

            // Confirm with query.
            int[] values = new int[1];
            EGL14.EglQueryContext(mEGLDisplay, mEGLContext, EGL14.EglContextClientVersion,
                    values, 0);
            Log.Debug(TAG, "EGLContext created, client version " + values[0]);
        }

        /**
         * Finds a suitable EGLConfig.
         *
         * @param flags Bit flags from constructor.
         * @param version Must be 2 or 3.
         */
        private EGLConfig getConfig(int flags, int version)
        {
            int renderableType = EGL14.EglOpenglEs2Bit;
            if (version >= 3)
            {
                renderableType |= EGLExt.EglOpenglEs3BitKhr;
            }

            // The actual surface is generally RGBA or RGBX, so situationally omitting alpha
            // doesn't really help.  It can also lead to a huge performance hit on glReadPixels()
            // when reading into a GL_RGBA buffer.
            int[] attribList = {
                EGL14.EglRedSize, 8,
                EGL14.EglGreenSize, 8,
                EGL14.EglBlueSize, 8,
                EGL14.EglAlphaSize, 8,
                //EGL14.EGL_DEPTH_SIZE, 16,
                //EGL14.EGL_STENCIL_SIZE, 8,
                EGL14.EglRenderableType, renderableType,
                EGL14.EglNone, 0,      // placeholder for recordable [@-3]
                EGL14.EglNone
        };
            if ((flags & FLAG_RECORDABLE) != 0)
            {
                attribList[attribList.Length - 3] = EGL_RECORDABLE_ANDROID;
                attribList[attribList.Length - 2] = 1;
            }
            EGLConfig[] configs = new EGLConfig[1];
            int[] numConfigs = new int[1];
            if (!EGL14.EglChooseConfig(mEGLDisplay, attribList, 0, configs, 0, configs.Length,
                    numConfigs, 0))
            {
                Log.Warn(TAG, "unable to find RGB8888 / " + version + " EGLConfig");
                return null;
            }
            return configs[0];
        }

        /**
         * Discards all resources held by this class, notably the EGL context.  This must be
         * called from the thread where the context was created.
         * <p>
         * On completion, no context will be current.
         */
        public void release()
        {
            if (mEGLDisplay != EGL14.EglNoDisplay)
            {
                // Android is unusual in that it uses a reference-counted EGLDisplay.  So for
                // every eglInitialize() we need an eglTerminate().
                EGL14.EglMakeCurrent(mEGLDisplay, EGL14.EglNoSurface, EGL14.EglNoSurface,
                        EGL14.EglNoContext);
                EGL14.EglDestroyContext(mEGLDisplay, mEGLContext);
                EGL14.EglReleaseThread();
                EGL14.EglTerminate(mEGLDisplay);
            }

            mEGLDisplay = EGL14.EglNoDisplay;
            mEGLContext = EGL14.EglNoContext;
            mEGLConfig = null;
        }

        
        protected override void JavaFinalize()
        {
            try {
                if (mEGLDisplay != EGL14.EglNoDisplay)
                {
                    // We're limited here -- finalizers don't run on the thread that holds
                    // the EGL state, so if a surface or context is still current on another
                    // thread we can't fully release it here.  Exceptions thrown from here
                    // are quietly discarded.  Complain in the log file.
                    Log.Warn(TAG, "WARNING: EglCore was not explicitly released -- state may be leaked");
                    release();
                }
            } finally {
                base.JavaFinalize();
            }
        }

        /**
         * Destroys the specified surface.  Note the EGLSurface won't actually be destroyed if it's
         * still current in a context.
         */
        public void releaseSurface(EGLSurface eglSurface)
        {
            EGL14.EglDestroySurface(mEGLDisplay, eglSurface);
        }

        /**
         * Creates an EGL surface associated with a Surface.
         * <p>
         * If this is destined for MediaCodec, the EGLConfig should have the "recordable" attribute.
         */
        public EGLSurface createWindowSurface(Java.Lang.Object surface)
        {
            if (!(surface is Surface) && !(surface is SurfaceTexture)) {
                throw new Exception("invalid surface: " + surface);
            }

            // Create a window surface, and attach it to the Surface we received.
            int[] surfaceAttribs = {
                EGL14.EglNone
        };
            EGLSurface eglSurface = EGL14.EglCreateWindowSurface(mEGLDisplay, mEGLConfig, surface,
                    surfaceAttribs, 0);
            checkEglError("eglCreateWindowSurface");
            if (eglSurface == null)
            {
                throw new Exception("surface was null");
            }
            return eglSurface;
        }

        /**
         * Creates an EGL surface associated with an offscreen buffer.
         */
        public EGLSurface createOffscreenSurface(int width, int height)
        {
            int[] surfaceAttribs = {
                EGL14.EglWidth, width,
                EGL14.EglHeight, height,
                EGL14.EglNone
        };
            EGLSurface eglSurface = EGL14.EglCreatePbufferSurface(mEGLDisplay, mEGLConfig,
                    surfaceAttribs, 0);
            checkEglError("eglCreatePbufferSurface");
            if (eglSurface == null)
            {
                throw new Exception("surface was null");
            }
            return eglSurface;
        }

        /**
         * Makes our EGL context current, using the supplied surface for both "draw" and "read".
         */
        public void makeCurrent(EGLSurface eglSurface)
        {
            if (mEGLDisplay == EGL14.EglNoDisplay)
            {
                // called makeCurrent() before create?
                Log.Debug(TAG, "NOTE: makeCurrent w/o display");
            }
            if (!EGL14.EglMakeCurrent(mEGLDisplay, eglSurface, eglSurface, mEGLContext))
            {
                throw new Exception("eglMakeCurrent failed");
            }
        }

        /**
         * Makes our EGL context current, using the supplied "draw" and "read" surfaces.
         */
        public void makeCurrent(EGLSurface drawSurface, EGLSurface readSurface)
        {
            if (mEGLDisplay == EGL14.EglNoDisplay)
            {
                // called makeCurrent() before create?
                Log.Debug(TAG, "NOTE: makeCurrent w/o display");
            }
            if (!EGL14.EglMakeCurrent(mEGLDisplay, drawSurface, readSurface, mEGLContext))
            {
                throw new Exception("eglMakeCurrent(draw,read) failed");
            }
        }

        /**
         * Makes no context current.
         */
        public void makeNothingCurrent()
        {
            if (!EGL14.EglMakeCurrent(mEGLDisplay, EGL14.EglNoSurface, EGL14.EglNoSurface,
                    EGL14.EglNoContext))
            {
                throw new Exception("eglMakeCurrent failed");
            }
        }

        /**
         * Calls eglSwapBuffers.  Use this to "publish" the current frame.
         *
         * @return false on failure
         */
        public bool swapBuffers(EGLSurface eglSurface)
        {
            return EGL14.EglSwapBuffers(mEGLDisplay, eglSurface);
        }

        /**
         * Sends the presentation time stamp to EGL.  Time is expressed in nanoseconds.
         */
        public void setPresentationTime(EGLSurface eglSurface, long nsecs)
        {
            EGLExt.EglPresentationTimeANDROID(mEGLDisplay, eglSurface, nsecs);
        }

        /**
         * Returns true if our context and the specified surface are current.
         */
        public bool isCurrent(EGLSurface eglSurface)
        {
            return mEGLContext.Equals(EGL14.EglGetCurrentContext()) &&
                eglSurface.Equals(EGL14.EglGetCurrentSurface(EGL14.EglDraw));
        }

        /**
         * Performs a simple surface query.
         */
        public int querySurface(EGLSurface eglSurface, int what)
        {
            int[] value = new int[1];
            EGL14.EglQuerySurface(mEGLDisplay, eglSurface, what, value, 0);
            return value[0];
        }

        /**
         * Queries a string value.
         */
        public string queryString(int what)
        {
            return EGL14.EglQueryString(mEGLDisplay, what);
        }

        /**
         * Returns the GLES version this context is configured for (currently 2 or 3).
         */
        public int getGlVersion()
        {
            return mGlVersion;
        }

        /**
         * Writes the current display, context, and surface to the log.
         */
        public static void logCurrent(string msg)
        {
            EGLDisplay display;
            EGLContext context;
            EGLSurface surface;

            display = EGL14.EglGetCurrentDisplay();
            context = EGL14.EglGetCurrentContext();
            surface = EGL14.EglGetCurrentSurface(EGL14.EglDraw);
            Log.Info(TAG, "Current EGL (" + msg + "): display=" + display + ", context=" + context +
                    ", surface=" + surface);
        }

        /**
         * Checks for EGL errors.  Throws an exception if an error has been raised.
         */
        private void checkEglError(string msg)
        {
            int error;
            if ((error = EGL14.EglGetError()) != EGL14.EglSuccess)
            {
                throw new Exception(msg + ": EGL error: 0x" + error.ToString());
            }
        }
    }
}
