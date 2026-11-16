using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImoutoPicsFinder.Danbooru;

public interface IDanbooruIqdbClient
{
    Task<IReadOnlyCollection<DanbooruIqdbMatch>> SearchFileAsync(Stream file);
}
