using System.ServiceModel;

namespace DgzAIOWindowsService
{
    [ServiceContract]
    public interface IAgentService
    {
        [OperationContract]
        void UpdateAgent(string zipPath);

        [OperationContract]
        void UninstallAgent();
    }
}
