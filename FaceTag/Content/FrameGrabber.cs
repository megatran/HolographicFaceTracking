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
        public struct Frame
        {
            // Simple data object that holds reference to the latest arrived frame for later analysis
            public MediaFrameReference mediaFrameReference;
            public SpatialCoordinateSystem spatialCoordinateSystem;
            public CameraIntrinsics cameraIntrinsics;
            public long timestamp;

        }

        //class attributes
        private MediaCapture mediaCapture;
        private MediaFrameSource mediaFrameSource;
        private MediaFrameReader mediaFrameReader;

        private Frame _lastFrame;

        public Frame LastFrame
        {
            get
            {
                lock (this)
                {
                    return _lastFrame;
                }
            }
            private set
            {
                lock(this)
                {
                    _lastFrame = value;
                }
            }
        }

        private DateTime _lastFrameCapturedTimeStamp = DateTime.MaxValue;

        public float ElapseTimeSinceLastFrameCaptured
        {
            get
            {
                return (float)(DateTime.Now - DateTime.MinValue).TotalMilliseconds;
            }
        }

        public bool IsValid
            // determine whether FrameGrabber is ready to avoid having other classes trying to pull frames before FrameGrabber is ready
        {
            get
            {
                return mediaFrameReader != null;
            }
        }

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

            if (status == MediaFrameReaderStartStatus.Success)
            {
                return new FrameGrabber(mediaCapture, selectedSource, mediaFrameReader);
            } else
            {
                return new FrameGrabber();
            }
        }

        void SetFrame(MediaFrameReference frame)
            // extract meta data from the captured frame and update the LastFrame property
        {
            var spatialCoordinateSystem = frame.CoordinateSystem;
            var cameraIntrinsics = frame.VideoMediaFrame.CameraIntrinsic;

            // mediaFrameReference (capturedFrame) // include location of camera & perspective projection 
            // of camera (coordiateSystem and CameraIntrinsic) to infer position of 
            // the camera in real world and augment it with digital content
            LastFrame = new Frame
            {
                mediaFrameReference = frame, 
                spatialCoordinateSystem = spatialCoordinateSystem,
                cameraIntrinsics = cameraIntrinsics,
                timestamp = Utils.GetCurrentUnixTimestampMillis()
            };

            _lastFrameCapturedTimeStamp = DateTime.Now;
        }

        private void MediaFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            MediaFrameReference frame = sender.TryAcquireLatestFrame();
            if (frame != null && frame.CoordinateSystem != null )
            {
                SetFrame(frame);
            }
        }
    }
}
