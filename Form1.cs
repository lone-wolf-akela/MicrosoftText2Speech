using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace MicrosoftText2Speech
{
    public partial class Form1 : Form
    {
        class KeyRegion
        {
            public string key { get; set; }
            public string region { get; set; }
        }
        class Voice
        {
            public List<string> styles { get; set; } = new();
        }
        class VoiceLang
        {
            public Dictionary<string, Voice> voices { get; set; } = new();
        }
        class VoiceData
        {
            public Dictionary<string, VoiceLang> langs { get; set; } = new();
        }
        public class MediaPlayer
        {
            System.Media.SoundPlayer soundPlayer;
            public MediaPlayer(byte[] buffer)
            {
                var memoryStream = new MemoryStream(buffer, true);
                soundPlayer = new System.Media.SoundPlayer(memoryStream);
            }
            public void Play()
            {
                soundPlayer.Play();
            }
            public void Play(byte[] buffer)
            {
                soundPlayer.Stream.Seek(0, SeekOrigin.Begin);
                soundPlayer.Stream.Write(buffer, 0, buffer.Length);
                soundPlayer.Play();
            }
        }

        KeyRegion keyregion;
        VoiceData voicedata;
        SpeechSynthesisResult audio;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if(File.Exists("key.json"))
            {
                keyregion = JsonSerializer.Deserialize<KeyRegion>(File.ReadAllText("key.json"));
                textBoxKey.Text = keyregion.key;
                textBoxRegion.Text = keyregion.region;
            }
            voicedata = JsonSerializer.Deserialize<VoiceData>(File.ReadAllText("voicedata.json"));
            comboBoxLang.Items.Clear();
            foreach(var lang in voicedata.langs.Keys)
            {
                comboBoxLang.Items.Add(lang);
            }
            comboBoxLang.SelectedIndex = 0;
        }

        private void buttonKeyRegion_Click(object sender, EventArgs e)
        {
            keyregion = new KeyRegion
            {
                key = textBoxKey.Text,
                region = textBoxRegion.Text
            };
            string keyregion_json = JsonSerializer.Serialize(keyregion);
            File.WriteAllText("key.json", keyregion_json);
            groupBox2.Enabled = true;
        }

        private void comboBoxLang_SelectedIndexChanged(object sender, EventArgs e)
        {
            var lang = voicedata.langs[comboBoxLang.SelectedItem as string];
            comboBoxVoice.Items.Clear();
            foreach (var voice in lang.voices.Keys)
            {
                comboBoxVoice.Items.Add(voice);
            }
            comboBoxVoice.SelectedIndex = 0;
            textChanged();
        }

        private void comboBoxVoice_SelectedIndexChanged(object sender, EventArgs e)
        {
            var voice = voicedata.langs[comboBoxLang.SelectedItem as string].voices
                [comboBoxVoice.SelectedItem as string];
            comboBoxStyle.Items.Clear();
            foreach(var style in voice.styles)
            {
                comboBoxStyle.Items.Add(style);
            }
            comboBoxStyle.SelectedIndex = 0;
            textChanged();
        }

        private async void buttonText2Speech_Click(object sender, EventArgs e)
        {
            buttonText2Speech.Enabled = false;
            labelStatus.Text = "正在生成...";
            var config = SpeechConfig.FromSubscription(keyregion.key, keyregion.region);
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
            using var synthesizer = new SpeechSynthesizer(config, null);
            string text = textBoxText.Text;
            if(comboBoxStyle.SelectedItem as string != "General")
            {
                text =
$@"<mstts:express-as style=""{comboBoxStyle.SelectedItem as string}"">
{text}
</mstts:express-as>";
            }
            text =
$@"<speak 
version=""1.0"" 
xmlns=""https://www.w3.org/2001/10/synthesis"" 
xmlns:mstts=""https://www.w3.org/2001/mstts""
xml:lang=""{comboBoxLang.SelectedItem as string}"">
<voice name=""{comboBoxVoice.SelectedItem as string}"">
<prosody rate=""{textBoxRate.Text}"">
{text}
</prosody>
</voice>
</speak>";
            audio = await synthesizer.SpeakSsmlAsync(text);
            if (audio.Reason == ResultReason.Canceled)
            {
                var reason = SpeechSynthesisCancellationDetails.FromResult(audio);
                labelStatus.Text = $"生成失败: {reason.ErrorDetails}";
            }
            else
            {
                labelStatus.Text = "生成完毕";
                buttonSave.Enabled = true;
                buttonPlay.Enabled = true;
            }
        }

        void textChanged()
        {
            if (textBoxText.Text != "")
            {
                buttonText2Speech.Enabled = true;
                buttonSave.Enabled = false;
                buttonPlay.Enabled = false;
            }
        }

        private void textBoxText_TextChanged(object sender, EventArgs e)
        {
            textChanged();
        }

        private async void buttonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "wav文件|*.wav";
            dialog.Title = "导出为wav文件";
            dialog.ShowDialog();
            if (dialog.FileName != "")
            {
                using var audiostream = AudioDataStream.FromResult(audio);
                await audiostream.SaveToWaveFileAsync(dialog.FileName);
                labelStatus.Text = "已保存";
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            MediaPlayer player = new(audio.AudioData);
            player.Play();
        }

        private void comboBoxStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            textChanged();
        }

        private void textBoxRate_TextChanged(object sender, EventArgs e)
        {
            textChanged();
        }
    }
}
