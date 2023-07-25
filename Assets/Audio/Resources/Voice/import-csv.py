import csv
import os
import sys
import torch
from bark import SAMPLE_RATE, generate_audio, preload_models
from scipy.io.wavfile import write as write_wav
from IPython.display import Audio
if torch.backends.mps.is_available():
    mps_device = torch.device("mps")
    x = torch.ones(1, device=mps_device)
    print (x)
else:
    print ("MPS device not found.")
os.environ["SUNO_ENABLE_MPS"] = "True"
preload_models()

# generate audio from text

  
# play text in notebook
#Audio(audio_array, rate=SAMPLE_RATE)




# os.chdir(os.path.dirname(sys.argv[0]))
#charMap={"Narr":"Allison","Dave":"Tom","Tony":"Alex","Neighbor2":"Tom",'HardwareClerk':"Oliver"}
charMap={"Narr":"en_speaker_9","Dave":"en_speaker_4","Tony":"en_speaker_1","Neighbor2":"it_speaker_en",'HardwareClerk':"hi_speaker_5"}
phaseMap={"English":"","PhaseOne":"P1/", "PhaseTwo":"P2/"}
input_file = csv.DictReader(open("CHFDialog.csv"))
for row in input_file:
    for phaseIndex, phaseName in enumerate(phaseMap): 

        # print ('say -v %s "%s" -o %s/%s%s.aiff'%(sys.argv[1],charMap[row['Character']], speechText,row['Character'],row['ID']))
        if row[phaseName]:
        
            text_prompt = row[phaseName]
            audio_array = generate_audio(text_prompt, history_prompt=f'v2/{charMap[row["Character"]]}')
            write_wav(f'{phaseMap[phaseName]}{row["Character"]}{row["ID"]}.wav', SAMPLE_RATE, audio_array)

 