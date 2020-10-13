using System.Drawing;

namespace Cupscale.Main
{
    public struct PreviewState
    {
        public Image image;
        public int zoom;
        public Point autoScrollPosition;

        public PreviewState(Image img, int currentZoom, Point autoScrollPos)
        {
            image = img;
            zoom = currentZoom;
            autoScrollPosition = autoScrollPos;
        }
    }
}