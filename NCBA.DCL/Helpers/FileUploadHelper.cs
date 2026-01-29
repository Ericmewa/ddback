namespace NCBA.DCL.Helpers;

public static class FileUploadHelper
{
    public static async Task<string> SaveFileAsync(IFormFile file, string uploadPath)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        // Ensure upload directory exists
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        // Generate unique filename
        var uniqueFileName = $"{DateTime.Now.Ticks}-{file.FileName}";
        var filePath = Path.Combine(uploadPath, uniqueFileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return uniqueFileName;
    }

    public static bool DeleteFile(string uploadPath, string fileName)
    {
        try
        {
            var filePath = Path.Combine(uploadPath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
