using AudioLab.Effects.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace AudioLab.Effects
{
	public sealed class AudioSpectrumExtractor : IBasicAudioEffect
	{
		private AudioEncodingProperties currentEncodingProperties;
		private List<AudioEncodingProperties> supportedEncodingProperties;

		private IPropertySet propertySet;
		public bool UseInputFrameForOutput { get { return false; } }
		public bool TimeIndependent { get { return false; } }
		public bool IsReadyOnly { get { return true; } }

		// Set up constant members in the constructor
		public AudioSpectrumExtractor()
		{
			supportedEncodingProperties = new List<AudioEncodingProperties>();
			AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 2, 32);
			encodingProps1.Subtype = MediaEncodingSubtypes.Float;
			AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 2, 32);
			encodingProps2.Subtype = MediaEncodingSubtypes.Float;

			supportedEncodingProperties.Add(encodingProps1);
			supportedEncodingProperties.Add(encodingProps2);
		}

		public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties
		{
			get
			{
				return supportedEncodingProperties;
			}
		}

		public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
		{
			currentEncodingProperties = encodingProperties;
			propertySet["chart"] = _chart;
		}

		float[] _chart = null;

		unsafe public void ProcessFrame(ProcessAudioFrameContext context)
		{
			AudioFrame inputFrame = context.InputFrame;

			using (AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read))
			using (IMemoryBufferReference inputReference = inputBuffer.CreateReference())
			{
				byte* inputDataInBytes;
				uint inputCapacity;

				((IMemoryBufferByteAccess)inputReference).GetBuffer(out inputDataInBytes, out inputCapacity);

				float* inputDataInFloat = (float*)inputDataInBytes;

				float inputDataL;
				float inputDataR;

				// Process audio data
				int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

                if(_chart == null)
                {
                    _chart = new float[dataInFloatLength];
                    propertySet["chart"] = _chart;
                }
				for (int i = 0; i < dataInFloatLength; i += 2)
				{
					inputDataL = inputDataInFloat[i];
					inputDataR = inputDataInFloat[i + 1];
					_chart[i] = inputDataL;
					_chart[i + 1] = inputDataR;
				}

			}
		}


		public void Close(MediaEffectClosedReason reason)
		{
			// Clean-up any effect resources
			// This effect doesn't care about close, so there's nothing to do
		}

		public void DiscardQueuedFrames()
		{
		}

		public void SetProperties(IPropertySet configuration)
		{
			this.propertySet = configuration;
		}

	}
}
