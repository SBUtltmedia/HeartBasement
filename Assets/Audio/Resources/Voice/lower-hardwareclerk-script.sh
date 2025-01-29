for i in HardwareClerk*.ogg;                         
do ffmpeg -i "$i" -filter:a "volume=0.5" ./hardwareclerk/"$i";
done
