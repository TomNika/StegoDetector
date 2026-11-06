using Microsoft.Win32; 

namespace StegoAnalysis.Models.Services
{
    public class ImageService
    {
        public string? OpenImageFileDialog()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.tif;*.gif;*.webp;*.ico;*.svg";
            if (dialog.ShowDialog() == true)
                return dialog.FileName;
            return null;
        }
    }
}