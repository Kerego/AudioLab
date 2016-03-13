using AudioLab.Effects.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace AudioLab.Effects
{
	public sealed class AudioBalanceEffect : IBasicAudioEffect
	{
		private AudioEncodingProperties currentEncodingProperties;
		private List<AudioEncodingProperties> supportedEncodingProperties;
		
		private IPropertySet propertySet;
		
		private float Balance
		{
			get { return (float)propertySet["Balance"]; }
		}

		public bool UseInputFrameForOutput { get { return false; } }
		public bool TimeIndependent { get { return false; } }
		public bool IsReadyOnly { get { return true; } }

		// Set up constant members in the constructor
		public AudioBalanceEffect()
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
		}

		unsafe public void ProcessFrame(ProcessAudioFrameContext context)
		{
			AudioFrame inputFrame = context.InputFrame;
			AudioFrame outputFrame = context.OutputFrame;

			using (AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read),
								outputBuffer = outputFrame.LockBuffer(AudioBufferAccessMode.Write))
			using (IMemoryBufferReference inputReference = inputBuffer.CreateReference(),
											outputReference = outputBuffer.CreateReference())
			{
				byte* inputDataInBytes;
				byte* outputDataInBytes;
				uint inputCapacity;
				uint outputCapacity;

				((IMemoryBufferByteAccess)inputReference).GetBuffer(out inputDataInBytes, out inputCapacity);
				((IMemoryBufferByteAccess)outputReference).GetBuffer(out outputDataInBytes, out outputCapacity);

				float* inputDataInFloat = (float*)inputDataInBytes;
				float* outputDataInFloat = (float*)outputDataInBytes;

				float inputDataL;
				float inputDataR;

				// Process audio data
				int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

				for (int i = 0; i < dataInFloatLength; i+=2)
				{
					inputDataL = inputDataInFloat[i] * ((Balance < 0) ? 1 : (1.0f - this.Balance));
					inputDataR = inputDataInFloat[i + 1] * ((Balance > 0) ? 1 : (1.0f + this.Balance));

					outputDataInFloat[i] = inputDataL;
					outputDataInFloat[i + 1] = inputDataR;
					
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
