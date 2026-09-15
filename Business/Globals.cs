using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Business
{
    public class Globals
    {
        [GroupDefinitions]
        public static class GroupNames
        {
            [Display(
                Name = "Metadata",
                Order = 40
            )]
            public const string Metadata = "Metadata";

            [Display(
                Name = "Specialized",
                Order = 40
            )]
            public const string Specialized = "Specialized";
        }
    }
}