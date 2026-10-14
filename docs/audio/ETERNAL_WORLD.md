# 로비 BGM: Eternal World (Instrumental)

v1.0.4부터 로비는 **Eternal World의 정식 무보컬 연주판**을 사용한다. 기존 Summertime·Golden Willow·Red Heart 음원과 원정의 무보컬 음악들은 보존한다.

- [공개 정식 배포 음원](https://www.youtube.com/watch?v=zwdsLONPlkQ): `Eternal World (Instrumental)`, Eternal Return, Release - Topic. 공개 재생 정보는 iMusician Digital AG 배포 및 ℗ Nimble Neuron을 명시한다.
- [님블뉴런의 공식 Steam 사운드트랙](https://store.steampowered.com/app/1300410/Eternal_Return_Soundtrack/?l=koreana)은 `Eternal world (Inst ver)`를 3분 36초 길이의 곡으로 수록한다.
- 보컬 삭제나 AI 재작곡을 거치지 않은 원래 연주판이다. 인게임 로비에서만 사용하며 음악 음량 설정과 장면 전환 처리는 기존 시스템을 따른다.
- 권리 표기: Eternal Return © Nimble Neuron Corporation. All rights reserved. 기존 님블뉴런 IP 이용 정책과 비공식 팬 게임 고지를 유지한다.

## 확보 및 변환

2026-10-14에 로그인, 쿠키, 인증 정보, DRM 도구 또는 접근 제한 우회 없이 공개 재생 오디오 형식 `251`(WebM/Opus)을 받았다. yt-dlp 2026.8.19를 임시 폴더에 설치했으며 이 도구는 게임에 포함하지 않는다. 유료 Steam 사운드트랙 파일을 내려받거나 구매하지 않았다.

Unity에서 사용할 파일은 [music_eternal_world.ogg](../../Assets/Resources/Lumia/Audio/Music/music_eternal_world.ogg)이다. FFmpeg/libvorbis 품질 6, 44,100 Hz 스테레오로 변환했다. 코덱 변환 후 피크가 1을 넘지 않게 고정 배율 0.8(-1.938 dB)만 적용하고, 시간 정보는 디코딩된 샘플 순서로 다시 기록했다. 곡의 구간, 속도, 편곡은 바꾸지 않았다.

```powershell
python -m yt_dlp --format 251 --no-playlist --output "Temp/AudioAcquisition/eternal_world_instrumental.%(ext)s" "https://www.youtube.com/watch?v=zwdsLONPlkQ"
Tools/.runtime/ffmpeg.exe -y -i Temp/AudioAcquisition/eternal_world_instrumental.webm -vn -map_metadata -1 -af "volume=0.8,asetpts=N/SR/TB" -c:a libvorbis -q:a 6 -ar 44100 -ac 2 -fflags +bitexact -flags:a +bitexact Assets/Resources/Lumia/Audio/Music/music_eternal_world.ogg
```

원본 공개 스트림 SHA-256은 `8ec2b47598bca1b53c65f59e2240c438f1745f738f8f80a7af1570eabab2b1d5`(4,104,201 bytes)이며, 게임 음원 SHA-256은 `1af8cc69140f01cbe9382082f9acf6e77fdace3c311d2e3d302f869147ba4d30`(4,929,971 bytes)이다. 유효기간이 있는 스트림 URL은 저장하지 않았다.

## 파일 검사

완성된 OGG를 전체 디코딩하여 **216.169773초**, 스테레오 44,100 Hz, 피크 **0.88741404**, RMS **0.21942185**, 클리핑 샘플 **0**을 확인했다. Unity 메타데이터는 기존 음악과 동일하게 스트리밍·백그라운드 로딩·자동 정규화 없음으로 지정했다.

출처·공개 재생 메타데이터·해시·변환 내역은 [eternal-world-source.json](eternal-world-source.json)과 [music-sources.json](music-sources.json)에 보관한다. 임시 청음용 30초 파일은 `Temp/AudioAcquisition/eternal-world-preview.ogg`이며 배포 게임에는 포함하지 않는다.
