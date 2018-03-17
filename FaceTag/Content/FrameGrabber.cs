using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices.Core;
using Windows.Perception.Spatial;

namespace FaceTag.Content
{
    class FrameGrabber
    {
        private MediaCapture mediaCapture;
        private MediaFrameSource mediaFrameSource;
        private MediaFrameReader mediaFrameReader;

        private FrameGrabber(MediaCapture mediaCapture = null, MediaFrameSource mediaFrameSource = null, MediaFrameReader mediaFrameReader - null)
        {
            this.mediaCapture = mediaCapture; // capture audio, video , and image from camera
            this.mediaFrameSource = mediaFrameSource; // source of camera fames (color camera in this case)
            this.mediaFrameReader = mediaFrameReader; // access to frames from a MediaFrameSource then notifies when new frame arrives

            if (this.mediaFrameReader != null)
            {
                this.mediaFrameReader.FrameArrived += MediaFrameReader_FrameArrived;
            }
        }

        private void MediaFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            throw new NotImplementedException();
        }

        public static async Task<FrameGrabber> CreateAsync()
        {
            MediaCapture mediaCapture = null;
            MediaFrameReader mediaFrameReader = null;
            MediaFrameSourceGroup selectedGroup = null;
            MediaFrameSourceInfo selectedSourceInfo = null;

            var groups = await MediaFrameSourceGroup.FindAllAsync();
            foreach (MediaFrameSourceGroup sourceGroup in groups)
            {
                // there should be only one color source for the HoloLens
                foreach (MediaFrameSourceInfo sourceInfo in sourceGroup.SourceInfos)
                {
                    if (sourceInfo.SourceKind == MediaFrameSourceKind.Color)
                    {
                        selectedSourceInfo = sourceInfo;
                        break;
                    }

                }
                if (selectedSourceInfo != null)
                {
                    selectedGroup = sourceGroup;
                }
            }

            // define the type of MediaCapture we want (Initialize MediaCapture to capture video from a color camera on teh CPU)
            var settings = new MediaCaptureInitializationSettings
            {
                SourceGroup = selectedGroup,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,

            };

            mediaCapture = new MediaCapture();

            try
            {
                await mediaCapture.InitializeAsync(settings);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Can't initialize MediaCapture {e.ToString()}");
                return new FrameGrabber();
            }

            // if initialization is successful, obtain MediaFrameSource and create MediaFrameReader
            MediaFrameSource selectedSource = mediaCapture.FrameSources[selectedSourceInfo.Id];
            mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(selectedSource);

            // ensure MediaFrameReader is successfully created to instantiate Grabber instance
            MediaFrameReaderStartStatus status = await mediaFrameReader.StartAsync();
        }
    }
}
