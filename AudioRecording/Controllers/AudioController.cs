using Microsoft.AspNetCore.Mvc;
using NAudio.Wave;

namespace AudioRecording.Controllers
{
    public class AudioController : Controller
    {
        private MemoryStream memoryStream;
        private WaveInEvent waveIn;

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult StartRecording()
        {
            memoryStream = new MemoryStream();
            waveIn = new WaveInEvent();

            // Configure WaveInEvent
            waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz sampling rate, mono (1 channel)
            waveIn.DataAvailable += (sender, e) =>
            {
                // Write recorded audio to the MemoryStream
                memoryStream.Write(e.Buffer, 0, e.BytesRecorded);
            };

            // Start recording
            waveIn.StartRecording();

            // Set up a timer to stop recording after 10 seconds
            Task.Delay(10000).ContinueWith(_ => StopRecording());

            return Ok(); // Indicate successful start of recording
        }

        [HttpPost]
        public IActionResult StopRecording()
        {
            if (waveIn != null)
            {
                // Stop recording
                waveIn.StopRecording();
                waveIn.Dispose();
                // Write the WAV header to the beginning of the MemoryStream
                WriteWavHeader(memoryStream);

                // Perform further processing or storage as needed
                string encodedAudio = ConvertToBase64(memoryStream.ToArray());
                ViewBag.EncodedAudio = encodedAudio;
           //     displaydata(encodedAudio);

            }

            return Ok(); // Indicate successful stop of recording
        }

        [HttpGet]
        public IActionResult displaydata(string encodedAudio)
        {
            ViewBag.EncodedAudio = encodedAudio;
            return View();
        }

        private void WriteWavHeader(MemoryStream stream)
        {
            if (stream != null)
            {
                // Get the audio data length
                long audioDataLength = stream.Length - 44; // Subtracting the length of the header (44 bytes)

                // Write the WAV header to the beginning of the MemoryStream
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Seek(0, SeekOrigin.Begin);
                writer.Write(0x46464952); // "RIFF" in ASCII
                writer.Write((int)(audioDataLength + 36)); // File size - 8
                writer.Write(0x45564157); // "WAVE" in ASCII
                writer.Write(0x20746D66); // "fmt " in ASCII
                writer.Write((int)16); // Subchunk1Size: PCM format (16)
                writer.Write((short)1); // AudioFormat: PCM
                writer.Write((short)1); // NumChannels: Mono (1 channel)
                writer.Write((int)16000); // SampleRate: 16kHz
                writer.Write((int)(16000 * 1 * 16 / 8)); // ByteRate: SampleRate * NumChannels * BitsPerSample / 8
                writer.Write((short)(1 * 16 / 8)); // BlockAlign: NumChannels * BitsPerSample / 8
                writer.Write((short)16); // BitsPerSample: 16
                writer.Write(0x61746164); // "data" in ASCII
                writer.Write((int)audioDataLength); // Subchunk2Size: audio data length
                writer.Seek(0, SeekOrigin.End); // Seek to the end of the MemoryStream
            }
        }

        private string ConvertToBase64(byte[] audioData)
        {
            // Convert to Base64
            return Convert.ToBase64String(audioData);
        }
    }
}
