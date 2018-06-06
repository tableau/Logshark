using Logshark.Core.Exceptions;

namespace Logshark.Core.Controller.Parsing
{
    internal interface IParsedLogsetValidator
    {
        /// <summary>
        /// Validates that a given processed logset yielded at least one document.
        /// </summary>
        /// <param name="logsetHash">The logset hash of the processed logset to check.</param>
        /// <exception cref="ProcessingException">If processed logset yielded no documents, a ProcessingException will be thrown.</exception>
        void ValidateDataExists(string logsetHash);
    }
}