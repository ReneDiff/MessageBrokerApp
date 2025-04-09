using MessageShared;
using System.Threading.Tasks;

namespace MessageConsumer;

// Abstraktion for at gemme beskeder i databasen.

public interface IDatabaseService
{
    Task SaveMessageAsync(Message message);
}