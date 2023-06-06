using System;

namespace Grafika.GLES
{
    /**
     * Off-screen EGL surface (pbuffer).
     * <p>
     * It's good practice to explicitly release() the surface, preferably from a "finally" block.
     */
    public class OffscreenSurface : EglSurfaceBase
    {
        /**
         * Creates an off-screen surface with the specified width and height.
         */
        public OffscreenSurface(EglCore eglCore, int width, int height) : base(eglCore)
        {
            createOffscreenSurface(width, height);
        }

        /**
         * Releases any resources associated with the surface.
         */
        public void release()
        {
            releaseEglSurface();
        }
    }
}
