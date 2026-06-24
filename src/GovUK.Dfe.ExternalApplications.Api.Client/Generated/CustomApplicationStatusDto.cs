namespace GovUK.Dfe.ExternalApplications.Api.Client.Contracts
{
    public partial class CustomApplicationStatusDto
    {
        public System.Guid? CustomApplicationStatusId { get; set; }
        public System.Guid TemplateId { get; set; }
        public int ApplicationStatus { get; set; }
        public string Label { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public System.Guid CreatedBy { get; set; }
    }
}
