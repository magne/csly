using System.Collections.Generic;
using System.Linq;

namespace sly.buildresult
{
    public class BuildResult<TResult>
    {
        public BuildResult() : this(default(TResult))
        { }

        public BuildResult(TResult result)
        {
            Result = result;
            Errors = new List<InitializationError>();
        }

        public List<InitializationError> Errors { get; }

        public TResult Result { get; set; }

        public bool IsError
        {
            get { return Errors.Any(e => e.Level != ErrorLevel.WARN); }
        }

        public bool IsOk => !IsError;

        public void AddError(InitializationError error)
        {
            Errors.Add(error);
        }

        public void AddErrors(List<InitializationError> errors)
        {
            Errors.AddRange(errors);
        }
    }
}