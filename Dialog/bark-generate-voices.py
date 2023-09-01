import csv
import os
import sys
import torch
import numpy as np
from bark.generation import (
    generate_text_semantic,
    preload_models,
)
from bark import SAMPLE_RATE ,generate_audio, preload_models
from bark.api import semantic_to_waveform
from scipy.io.wavfile import write as write_wav
from IPython.display import Audio
os.chdir(os.path.dirname(sys.argv[0]))
silence = np.zeros(int(0.5 * SAMPLE_RATE)) 
GEN_TEMP = 0.7
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

charMap={"Narr":"en_speaker_9","Dave":"en_speaker_6","Tony":"en_speaker_1","Neighbor2":"it_speaker_en",'HardwareClerk':"en_speaker_4"}
phaseMap={"English":"","PhaseOne":"P1/", "PhaseTwo":"P2/"}
input_file = csv.DictReader(open("CHFDialog.csv"))
for row in input_file:
	speaker=f'v2/{charMap[row["Character"]]}'
	bias = ""
	if (row["Character"] == "Narr"):
		bias = ""
	sentence = f'{row["English"]}'.strip()
	print("*",sentence,"*")
	semantic_tokens = generate_text_semantic(
        sentence,
        history_prompt=speaker,
        temp=GEN_TEMP,
        min_eos_p=0.05,  # this controls how likely the generation is to end
    )
	
	filename = f'{PATH}{row["Character"]}{row["ID"]}'
	if RUN_BARK and row["Character"]=="Tony":
		audio_array = semantic_to_waveform(semantic_tokens, history_prompt=speaker)
		write_wav(filename+'.wav', SAMPLE_RATE, audio_array)

	os.system(f'ffmpeg -y -i {filename}.wav -filter:a "volume=2" {filename}.ogg')
	os.remove(filename+'.wav')

