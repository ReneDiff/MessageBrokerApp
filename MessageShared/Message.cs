namespace MessageShared;

// Repræsenterer en besked i systemet med et timestamp og en tællerværdi.
public class Message
{
    // Tidspunktet hvor beskeden blev sendt.
    public DateTime Timestamp {get; set;}

    // En tæller, der øges hvis beskeden requeues.
    public int Counter {get; set;}
}
