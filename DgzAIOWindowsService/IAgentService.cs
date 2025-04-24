using System.ServiceModel;

namespace DgzAIOWindowsService
{
    [ServiceContract]
    public interface IAgentService
    {
        [OperationContract]
        void UpdateAgent(string zipPath, string localPath);

        [OperationContract]
        void UninstallAgent();
    }
}
