using System.Threading.Tasks;
using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates a real embedding vector for the given text using the specified embedding model.
        /// </summary>
        Task<float[]> GetEmbeddingAsync(string text, EmbeddingModel model);

        /// <summary>
        /// Gets the vector dimensions for a given embedding model.
        /// </summary>
        int GetEmbeddingDimensions(EmbeddingModel model);
    }
}
