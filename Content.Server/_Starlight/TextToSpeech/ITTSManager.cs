using System.Threading.Tasks;  
using Content.Shared.Starlight.TextToSpeech;  
  
namespace Content.Server.Starlight.TextToSpeech;  
public interface ITTSManager  
{  
    Task<byte[]?> ConvertTextToSpeechAnnounce(string voiceId, string text);  
    Task<byte[]?> ConvertTextToSpeechRadio(string voiceId, string text);  
    Task<byte[]?> ConvertTextToSpeechStandard(string voiceId, string text);  
    void Initialize();  
}