namespace Api.Web.Response
{
    public class Response<T> : Response
    {
        public T Data { get; set; }
        public Response(T data, string errorMessage = null) : base(errorMessage)
        {
            Data = data;
        }
    }

    public class Response
    {
        public string ErrorMessage { get; set; }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public Response(string errorMessage = null)
        {
            ErrorMessage = errorMessage;
        }
    }
}
