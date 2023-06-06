using System;
using Android.Graphics;
using Android.Views;

namespace Grafika.GLES
{
    public class WindowSurface : EglSurfaceBase
    {
        private Surface mSurface;
        private bool mReleaseSurface;

        /**
         * Associates an EGL surface with the native window surface.
         * <p>
         * Set releaseSurface to true if you want the Surface to be released when release() is
         * called.  This is convenient, but can interfere with framework classes that expect to
         * manage the Surface themselves (e.g. if you release a SurfaceView's Surface, the
         * surfaceDestroyed() callback won't fire).
         */
        public WindowSurface(EglCore eglCore, Surface surface, bool releaseSurface) : base(eglCore)
        {
            createWindowSurface(surface);
            mSurface = surface;
            mReleaseSurface = releaseSurface;
        }

        /**
         * Associates an EGL surface with the SurfaceTexture.
         */
        public WindowSurface(EglCore eglCore, SurfaceTexture surfaceTexture) : base(eglCore)
        {
            createWindowSurface(surfaceTexture);
        }

        /**
         * Releases any resources associated with the EGL surface (and, if configured to do so,
         * with the Surface as well).
         * <p>
         * Does not require that the surface's EGL context be current.
         */
        public void release()
        {
            releaseEglSurface();
            if (mSurface != null)
            {
                if (mReleaseSurface)
                {
                    mSurface.Release();
                }
                mSurface = null;
            }
        }

        /**
         * Recreate the EGLSurface, using the new EglBase.  The caller should have already
         * freed the old EGLSurface with releaseEglSurface().
         * <p>
         * This is useful when we want to update the EGLSurface associated with a Surface.
         * For example, if we want to share with a different EGLContext, which can only
         * be done by tearing down and recreating the context.  (That's handled by the caller;
         * this just creates a new EGLSurface for the Surface we were handed earlier.)
         * <p>
         * If the previous EGLSurface isn't fully destroyed, e.g. it's still current on a
         * context somewhere, the create call will fail with complaints from the Surface
         * about already being connected.
         */
        public void recreate(EglCore newEglCore)
        {
            if (mSurface == null)
            {
                throw new Exception("not yet implemented for SurfaceTexture");
            }
            mEglCore = newEglCore;          // switch to new context
            createWindowSurface(mSurface);  // create new surface
        }
    }
}