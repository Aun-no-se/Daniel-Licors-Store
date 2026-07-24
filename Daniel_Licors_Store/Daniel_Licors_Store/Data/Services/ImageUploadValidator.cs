namespace Daniel_Licors_Store.Data.Services
{
    /// <summary>
    /// Validación centralizada de imágenes subidas por el usuario, usada por
    /// Combos, Promotions y ProductDialog.
    ///
    /// Por qué existe: antes, cada lugar decidía la extensión del archivo
    /// guardado a partir del NOMBRE que manda el navegador
    /// (Path.GetExtension(file.Name)), y como mucho revisaba el
    /// Content-Type — pero tanto el nombre del archivo como el Content-Type
    /// los controla por completo quien sube el archivo, y se falsifican con
    /// cualquier herramienta (o simplemente editando el nombre antes de
    /// subirlo). Eso permitía, en teoría, guardar un archivo llamado
    /// "algo.html" con una imagen falsa de por medio, servible luego
    /// directamente desde wwwroot/images.
    ///
    /// Esta clase en cambio abre los primeros bytes del archivo y verifica
    /// su firma real (magic number) contra JPEG/PNG/WEBP. La extensión con
    /// la que se guarda el archivo sale de esa firma, nunca del nombre
    /// original ni del Content-Type declarado.
    /// </summary>
    public static class ImageUploadValidator
    {
        public const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

        public record ValidationResult(bool IsValid, string? Extension, string? ErrorMessage)
        {
            public static ValidationResult Fail(string message) => new(false, null, message);
            public static ValidationResult Ok(string extension) => new(true, extension, null);
        }

        /// <summary>
        /// Valida el tamaño y la firma real de los bytes del archivo.
        /// Necesita al menos los primeros 12 bytes del archivo para
        /// reconocer JPEG, PNG o WEBP — si le pasas el archivo completo
        /// (recomendado, para poder guardarlo después) funciona igual.
        /// </summary>
        public static ValidationResult Validate(byte[] fileBytes, long declaredSize)
        {
            if (declaredSize <= 0 || fileBytes.Length == 0)
                return ValidationResult.Fail("El archivo está vacío.");

            if (declaredSize > MaxSizeBytes)
                return ValidationResult.Fail("La imagen no puede superar 5 MB.");

            // JPEG: FF D8 FF
            if (fileBytes.Length >= 3 &&
                fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
                return ValidationResult.Ok(".jpg");

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (fileBytes.Length >= 8 &&
                fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47 &&
                fileBytes[4] == 0x0D && fileBytes[5] == 0x0A && fileBytes[6] == 0x1A && fileBytes[7] == 0x0A)
                return ValidationResult.Ok(".png");

            // WEBP: "RIFF" (bytes 0-3) + tamaño (bytes 4-7) + "WEBP" (bytes 8-11)
            if (fileBytes.Length >= 12 &&
                fileBytes[0] == 0x52 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x46 &&
                fileBytes[8] == 0x57 && fileBytes[9] == 0x45 && fileBytes[10] == 0x42 && fileBytes[11] == 0x50)
                return ValidationResult.Ok(".webp");

            return ValidationResult.Fail("El archivo no es una imagen JPEG, PNG o WEBP válida.");
        }
    }
}
