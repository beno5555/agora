namespace colosseumbackend.Common;

public class ApplicationResponse
{
    public bool    Succeeded { get; set; } = true;
    public string? Message   { get; set; }

    public void Ok(string? message = null)
    {
        Succeeded = true;
        Message   = message;
    }

    public void Fail(string? message = null)
    {
        Succeeded = false;
        Message   = message;
    }
}

public class ApplicationResponse<T> : ApplicationResponse
{
    public T? Data { get; set; }

    public void Ok(T data, string? message = null)
    {
        Ok(message);
        Data = data;
    }
}

public class ListResponse<T> : ApplicationResponse
{
    public List<T> Data { get; set; } = [];

    public void Ok(List<T> data, string? message = null)
    {
        Ok(message);
        Data = data;
    }
}
