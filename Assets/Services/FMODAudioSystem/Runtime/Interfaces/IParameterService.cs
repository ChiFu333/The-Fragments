using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;

namespace Services.FMODAudioSystem
{
    public interface IParameterService
    {
        void SetGlobal(string parameterName, float value);
        float GetGlobal(string parameterName);
        UniTask RampGlobal(string parameterName, float target, float duration);

        UniTask RampEvent(EventReference reference, string parameterName, float target, float duration);
        UniTask RampEvent(string name, string parameterName, float target, float duration);
        UniTask RampEvent(EventInstance instance, string parameterName, float target, float duration);
    }
}
