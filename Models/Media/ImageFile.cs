using EPiServer.Framework.DataAnnotations;

namespace Optimizely26.Models.Media
{
    [ContentType(GUID = "B3FE2ADD-56E6-4018-AD98-65F9330CFB3B")]
    [MediaDescriptor(ExtensionString = "jpg,jpeg,png,gif")]
    public class ImageFile : ImageData
    {
    }
}