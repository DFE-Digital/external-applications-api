namespace DfE.ExternalApplications.Application.Common.QueriesObjects
{
    public interface IQueryObject<T>
    {
        IQueryable<T> Apply(IQueryable<T> query);
    }
}
