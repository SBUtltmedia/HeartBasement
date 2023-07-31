import csv
import os
import sys
import torch
from bark import SAMPLE_RATE, generate_audio, preload_models
from scipy.io.wavfile import write as write_wav
from IPython.display import Audio
os.chdir(os.path.dirname(sys.argv[0]))
RUN_BARK=False
RUN_BARK=True
PATH = "../Assets/Audio/Resources/Voice/"
if RUN_BARK:
	if torch.backends.mps.is_available():
		mps_device = torch.device("mps")
		x = torch.ones(1, device=mps_device)
		print (x)
	else:
		print ("MPS device not found.")
	os.environ["SUNO_ENABLE_MPS"] ="true" 
	preload_models()

charMap={"Narr":"en_speaker_9","Dave":"en_speaker_1","Tony":"en_speaker_2","Neighbor2":"it_speaker_en",'HardwareClerk':"hi_speaker_5"}
phaseMap={"English":"","PhaseOne":"P1/", "PhaseTwo":"P2/"}
input_file = csv.DictReader(open("CHFDialog.csv"))
for row in input_file:
	bias = "MAN"
	if (row["Character"] == "Narr"):
		bias = "WOMAN"
	filename = f'{PATH}{row["Character"]}{row["ID"]}'
	if RUN_BARK:
		audio_array = generate_audio(f'[{bias}] {row["English"]}', history_prompt=f'v2/{charMap[row["Character"]]}')
		write_wav(filename+'.wav', SAMPLE_RATE, audio_array)

	os.system(f'ffmpeg -y -i {filename}.wav -filter:a "volume=2" {filename}.ogg')
	os.remove(filename+'.wav')

