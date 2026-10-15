using UnityEngine;
using UnityEngine.Serialization;

namespace Car
{
    [System.Serializable]
    public class EngineClip
    {
        [FormerlySerializedAs("refRPM")] public float refRpm; //RPM at which the clip is at 100% volume

        [HideInInspector]
        public AudioSource sourceOff;
        [HideInInspector]
        public AudioSource sourceOn;

        public AudioClip clipOff;
        public AudioClip clipOn;
        [HideInInspector]
        public bool noFallingEdge; //Tick to prevent audio clip's volume from falling off

        public EngineClip(AudioSource sourceOff, AudioSource sourceOn, AudioClip clipOff, AudioClip clipOn, float refRpm, bool noFallingEdge)
        {
            //Setters
            this.clipOff = clipOff;
            this.clipOn = clipOn;
            this.refRpm = refRpm;
            this.noFallingEdge = noFallingEdge;
            this.sourceOff = sourceOff;
            this.sourceOn = sourceOn;

            //Set sources' clips to the audio clips
            this.sourceOff.clip = this.clipOff;
            this.sourceOn.clip = this.clipOn;

            //Set the sources to loop
            this.sourceOff.loop = true;
            this.sourceOn.loop = true;

            //Set sources to play on startup
            this.sourceOff.Play();
            this.sourceOn.Play();

            //Set volume to 0 on startup
            this.sourceOff.volume = 0.0f;
            this.sourceOn.volume = 0.0f;
        }

        public float GetVolume(float inRpm, float range)
        {
            var min = refRpm - range;
            var max = refRpm + range;

            if ((inRpm < min || inRpm > max) && !noFallingEdge) //Set to zero if outside range
            {
                return 0.0f;
            }
            if (inRpm <= refRpm) //Rising Edge
            {
                return Mathf.Max((inRpm - min) / (refRpm - min), 0.0f);
            }

            //Falling Edge
            return !noFallingEdge ? Mathf.Max((max - inRpm) / (max - refRpm), 0.0f) : 1.0f;
        }
    }

    public class EngineAudio : MonoBehaviour
    {
        public Engine engine;
        public EngineClip[] engineClips; //Input in ascending order (ex: 1koff, 1kon -> 2koff, 2kon -> ....)
        private float _rpmInterval; //must be the same between all clips!!!

        public void Initialize()
        {
            var noFall = false;
            for (var i = 0; i < engineClips.Length; i++)
            {
                var newSourceOff = gameObject.AddComponent<AudioSource>();
                var newSourceOn = gameObject.AddComponent<AudioSource>();

                if (i == (engineClips.Length - 1)) //Make sure the final clip doesn't have a falloff in volume
                {
                    noFall = true;
                }
                engineClips[i] = new EngineClip(newSourceOff, newSourceOn, engineClips[i].clipOff, engineClips[i].clipOn, engineClips[i].refRpm, noFall);
            }

            _rpmInterval = engineClips[1].refRpm - engineClips[0].refRpm; //NOTE: RPM spacing between all clips must be the same!!!
        }

        public void UpdateAudio() //Use LateUpdate for processes that come after input and game logic
        {
            foreach (var t in engineClips)
            {
                //Reset source volume
                t.sourceOff.volume = 0.0f;
                t.sourceOn.volume = 0.0f;

                //Set audio pitch based off of its own reference
                t.sourceOff.pitch = engine.engineRpm / t.refRpm;
                t.sourceOn.pitch = engine.engineRpm / t.refRpm;

                //Set the volume of a clip based off the current engine speed and throttle input
                t.sourceOff.volume = t.GetVolume(engine.engineRpm, _rpmInterval) * (1.0f - engine.throttle);
                t.sourceOn.volume = t.GetVolume(engine.engineRpm, _rpmInterval) * engine.throttle;

                //Keep all sources (even silent ones) playing due to audio clipping issues when trying to pause or stop sources and replay them
                if (!t.sourceOff.isPlaying)
                {
                    t.sourceOff.Play();
                }
                if (!t.sourceOn.isPlaying)
                {
                    t.sourceOn.Play();
                }
            }
        }
    }
}