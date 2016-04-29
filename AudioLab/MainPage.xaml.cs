using AudioLab.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Audio;
using Windows.Media.Effects;
using Windows.Media.Render;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioLab
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{

		private float _part = 0;
		private float[] _current = new float[224];
		private float[] _prev = new float[224];
		private float _angle = 0;
		private float _step = 3.14f / 112;
		float[] _spectrum;

		private Size _canvasSize;
		private PropertySet _properties = new PropertySet();
		private AudioGraph _graph;
		private AudioFileInputNode _fileInputNode;
		private AudioDeviceOutputNode _deviceOutputNode;
		private AudioSubmixNode _subMixNode;

		private AudioEffectDefinition _noiseEffectDefinition;
		private EchoEffectDefinition _echoEffectDefinition;
		private AudioEffectDefinition _balanceEffectDefinition;
		private AudioEffectDefinition _spectrumExtractor;

		public MainPage()
		{
			this.InitializeComponent();
			Loaded += PageLoaded;
		}

		private async void PageLoaded(object sender, RoutedEventArgs e)
		{
			await CreateAudioGraph();
			_graph.Start();
			AddCustomEffect();
			_spectrum = _properties["chart"] as float[];

			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Tick += (s, args) =>
			{
				if (_fileInputNode == null)
					return;
				CurrentProgressPositionLabel.Text = _fileInputNode.Position.ToString(@"m\m\:s\s");
				Progress.Value = _fileInputNode.Position.TotalSeconds;
			};

			timer.Start();
		}

		private float Lerp(float a, float b, float t) => a + (b - a) * t;

		private async Task<CreateAudioFileInputNodeResult> SelectInputFile()
		{
			FileOpenPicker filePicker = new FileOpenPicker();
			filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
			filePicker.FileTypeFilter.Add(".mp3");
			filePicker.ViewMode = PickerViewMode.Thumbnail;
			StorageFile file = await filePicker.PickSingleFileAsync();

			// File can be null if cancel is hit in the file picker
			if (file == null)
			{
				return null;
			}

			CreateAudioFileInputNodeResult fileInputNodeResult = await _graph.CreateFileInputNodeAsync(file);
			if (fileInputNodeResult.Status != AudioFileNodeCreationStatus.Success)
			{
				// Cannot read file
				throw new Exception("error");
			}
			return fileInputNodeResult;
		}

		private async Task CreateAudioGraph()
		{
			// Create an AudioGraph with default settings
			AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
			CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

			if (result.Status != AudioGraphCreationStatus.Success)
			{
				// Cannot create graph
				throw new Exception("error");
			}

			_graph = result.Graph;

			// Create a device output node
			CreateAudioDeviceOutputNodeResult deviceOutputResult = await _graph.CreateDeviceOutputNodeAsync();
			_subMixNode = _graph.CreateSubmixNode();
			

			if (deviceOutputResult.Status != AudioDeviceNodeCreationStatus.Success)
			{
				// Cannot create device output
				throw new Exception("error");
			}

			_deviceOutputNode = deviceOutputResult.DeviceOutputNode;
			_subMixNode.AddOutgoingConnection(_deviceOutputNode);
		}

		private void AddCustomEffect()
		{
			_properties["Noise"] = 0.0f;
			_properties["Echo"] = 0.0f;
			_noiseEffectDefinition = new AudioEffectDefinition(typeof(AudioNoiseEffect).FullName, _properties);
			_properties["Echo Delay"] = 0.0f;
			_echoEffectDefinition = new EchoEffectDefinition(_graph);
			_properties["Balance"] = 0.0f;
			_balanceEffectDefinition = new AudioEffectDefinition(typeof(AudioBalanceEffect).FullName, _properties);
			_spectrumExtractor = new AudioEffectDefinition(typeof(AudioSpectrumExtractor).FullName, _properties);

			_echoEffectDefinition.Delay = 100;
			_echoEffectDefinition.Feedback = 0.5;
			_echoEffectDefinition.WetDryMix = 0;

			_subMixNode.EffectDefinitions.Add(_noiseEffectDefinition);
			_subMixNode.EffectDefinitions.Add(_balanceEffectDefinition);
			_subMixNode.EffectDefinitions.Add(_spectrumExtractor);
			_subMixNode.EffectDefinitions.Add(_echoEffectDefinition);
		}

		

		// Event handler for file completion event
		private void FileInput_FileCompleted(AudioFileInputNode sender, object args)
		{
			// File playback is done. Stop the graph
			_graph.Stop();
			// Reset the file input node so starting the graph will resume playback from beginning of the file
			sender.Reset();
		}

		private void PlayStopButtonClick(object sender, RoutedEventArgs e)
		{
			if(PlayButton.IsChecked.Value)
			{
				_graph.Start();
				PlayButton.Content = "\ue103";
			}
			else
			{
				_graph.Stop();
				PlayButton.Content = "\ue102";
			}
		}
		private void Canvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
		{
			if (!_properties.ContainsKey("chart"))
				return;
			_part += 0.1f;
			if (_part > 1)
			{
				_prev = _current;
				_current = new float[224];
				for (int i = 0; i < 224; i++)
					_current[i] = _spectrum.Skip(i * 4).Take(4).Average() * 200;
				_part = 0.1f;
			}

			_angle = 0;
			for (int i = 0; i < 224; i++)
			{
				var lerp = Lerp(_prev[i], _current[i], _part);
				var abs = Math.Abs(lerp);
				float Y2 = (float)(Math.Cos(_angle) * (abs + 100) + _canvasSize.Height / 2);
				float Y1 = (float)(Math.Cos(_angle) * 100 + _canvasSize.Height / 2);
				float X2 = (float)(Math.Sin(_angle) * (abs + 100) + _canvasSize.Width / 2);
				float X1 = (float)(Math.Sin(_angle) * 100 + _canvasSize.Width / 2);
				if(lerp > 0)
					args.DrawingSession.DrawLine(X1, Y1, X2, Y2, Color.FromArgb(
						255, 
						(byte)(((abs) / 200) * 255), 
						0,
						(byte)(((200 -abs) / 200) * 255)));
				else
					args.DrawingSession.DrawLine(X1, Y1, X2, Y2, Color.FromArgb(
						255, 
						0,
						(byte)(((abs) / 200) * 255),
						(byte)(((200 - abs) / 200) * 255)));
				_angle += _step;
			}
			//args.DrawingSession.FillCircle((float)canvasSize.Height / 2, (float)canvasSize.Width /2, 100, Colors.White);
		}
		private void Canvas_CreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
		{
			_canvasSize = sender.Size;
			sender.SizeChanged += Sender_SizeChanged;
			sender.ClearColor = Colors.Black;
		}

		private void Sender_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			_canvasSize = (sender as CanvasAnimatedControl).Size;
		}

		private void Progress_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (_fileInputNode == null)
				return;
			if (Math.Abs(_fileInputNode.Position.TotalSeconds - e.NewValue) < 0.5)
				return;
			_fileInputNode.Seek(TimeSpan.FromSeconds(e.NewValue));
		}

		private void PlaybackSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (_fileInputNode != null)
				_fileInputNode.PlaybackSpeedFactor = (float)e.NewValue;
		}

		private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (_fileInputNode != null)
				_fileInputNode.OutgoingGain = (float)e.NewValue;
		}

		private void EchoDelaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (_echoEffectDefinition != null)
				_echoEffectDefinition.Delay = e.NewValue;
		}
		private void EchoMixSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (_echoEffectDefinition != null)
				_echoEffectDefinition.WetDryMix = e.NewValue;
		}

		private void NoiseSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (_noiseEffectDefinition != null)
				_properties["Noise"] = (float)e.NewValue;
		}

		private void BalanceSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (_balanceEffectDefinition != null)
				_properties["Balance"] = (float)e.NewValue;
		}

		private async void FirstFileButtonClick(object sender, RoutedEventArgs e)
		{
			var fileInputNodeResult = await SelectInputFile();
			if (fileInputNodeResult == null)
				return;
			if(_fileInputNode != null)
			{
				_fileInputNode.FileCompleted -= FileInput_FileCompleted;
				_fileInputNode.Dispose();
			}
			_fileInputNode = fileInputNodeResult.FileInputNode;
			fileInputNodeResult.FileInputNode.AddOutgoingConnection(_subMixNode);

			Progress.Minimum = 0;
			Progress.Maximum = _fileInputNode.Duration.TotalSeconds - 1;
			DurationLabel.Text = _fileInputNode.Duration.ToString(@"m\m\:s\s");

			// Event Handler for file completion
			_fileInputNode.FileCompleted += FileInput_FileCompleted;
		}
		AudioFileInputNode _second;
		private async void SecondFileButtonClick(object sender, RoutedEventArgs e)
		{
			if (_second == null)
			{
				var fileInputNodeResult = await SelectInputFile();
				_second = fileInputNodeResult.FileInputNode;
				_second.AddOutgoingConnection(_subMixNode);
				SecondFileButton.Content = "Stop second file";
			}
			else if (_second.OutgoingConnections.Count >= 1)
			{
				_second.RemoveOutgoingConnection(_subMixNode);
				SecondFileButton.Content = "Resume second file";
			}
			else
			{
				_second.AddOutgoingConnection(_subMixNode);
				SecondFileButton.Content = "Stop second file";

			}
		}

		private void ControlElementClicked(object sender, RoutedEventArgs e)
		{
			FrameworkElement senderElement = sender as FrameworkElement;
			Flyout flyout = FlyoutBase.GetAttachedFlyout(senderElement) as Flyout;
			flyout.Placement = FlyoutPlacementMode.Top;
			flyout.ShowAt(senderElement);
		}
	}
}
