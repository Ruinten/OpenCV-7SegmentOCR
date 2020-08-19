# OpenCV-7SegmentOCR

액정화면에서 6자리 숫자를 인식하는 기능을 구현되어있다.
<div>
<img src="https://user-images.githubusercontent.com/31661769/90608750-ffc1e100-e23d-11ea-9076-3b9449fb2632.jpg" />    
</div>

### RuintenOCR 하위 폴더 구조

* result : 인식 성공한 이미지 (매번 성공할 때마다 모든 파일을 지우고 새로 생성)
* success : 인식 성공한 이미지 사본
* fail : 인식 실패한 이미지 파일
* exception : 에러 텍스트 파일

### 실행방법

exe 파일 실행(호출)

    .\ruintenocr.exe

### 성공인 경우 문자열 확인 방법

* ruintenocr\result 의 파일명(BMP 파일)
* 파일이 없으면 실패 (실행 초기에 파일을 모두 지우고 시작)

### 어플리케이션 설정 (ruintenocr.exe.config 파일 내용)

    <appSettings>
    <add key="RunMode" value=""/> <!--기본값: 빈 문자열, d:디버깅-->
    <add key="DeviceNumber" value="1"/><!--기본값: 0-->
    <add key="RunMaximum" value="10"/><!--최대 실행 횟수-->
    <add key="DetectWidth" value="175"/><!--검출영역 폭-->
    <add key="DetectHeight" value="45"/><!--검출영역 높이-->
    <add key="DeleteWidth" value="16"/><!--검출영역에서 잘라낼 앞부분 폭-->
    <add key="ThresholdValue" value="40"/><!--Binarize 임계치-->
    <add key="PathResult" value="result"/><!--결과 저장 경로-->
    <add key="PathSuccess" value="success"/><!--성공 사본 저장 경로-->
    <add key="PathFail" value="fail"/><!--실패 사본 저장 경로-->
    <add key="PathException" value="exception"/><!--Exception 저장 경로-->
    </appSettings> 

  
### 처리 과정

1. Windows 연결된 카메라에서 화상 읽어오기
2. 읽어온 화상에서 특정 크기(DetectWidth * DetectHeight)의 사각형 영역 검출
3. 검출된 사각 영역을 Grayscale 변환
4. Noise 제거
5. 잡영 제거
6. 경계 강화
7. 문자 인식
8. 인식 결과에서 숫자가 6개가 아니면 반복
9. 6개 숫자가 나오면 ruintenocr\result 에 검출된 숫자를 파일명으로 화상 저장

<div>
<img src="https://user-images.githubusercontent.com/31661769/90608748-ff294a80-e23d-11ea-895a-d14fc29bc232.png" />    
</div>
<div>
<img src="https://user-images.githubusercontent.com/31661769/90608743-fdf81d80-e23d-11ea-8030-8507ebd4fca3.png" />    
</div>
<div>
<img src="https://user-images.githubusercontent.com/31661769/90609538-2a606980-e23f-11ea-8d7a-666055f8cb55.png" />    
</div>

### 실행 환경

* OS : Windows 10 (.NET Framework 4.6 이상)
* 카메라 : 컴퓨터에 연결할 수 있으며 Windows 환경에서 동작하는 해상도 HD (1280 x 720) 이상의 웹캠.
* OTP 에 반사광 또는 진한 그림자가 생기지 않는 적당한 주변 조명.
* OTP 액정과 숫자의 구분이 명확할 수록 인식 소요 시간이 단축 가능.
* 카메라에 자동초점 기능이 없을 경우 최소 30cm 이상의 거리가 필요하며 화상에서의 OTP 크기가 작아지면 인식률이 저조할 수 있음.
* 주기적으로 success 폴더와 fail 폴더의 이미지를 확인하여 최적의 실행 환경 검토 필요.
