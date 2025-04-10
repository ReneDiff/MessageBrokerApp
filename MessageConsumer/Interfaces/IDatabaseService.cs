using MessageShared;
using System.Threading.Tasks;

namespace MessageConsumer.interfaces;

// Abstraktion for at gemme beskeder i databasen.

public interface IDatabaseService
{
    Task SaveMessageAsync(Message message);
}