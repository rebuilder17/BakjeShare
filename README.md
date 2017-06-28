# BakjeShare

DB 텀프로젝트입니다.

## 개발 환경

- Windows 8.1 64bit
- Visual Studio 2017
- Server side + Protocol
	- C# 6.0 + .Net 4.5 (No additional platforms needed)
- Client side
	- Xamarin 2.x (Used Xamarin.Forms but only targets to Android)

## 프로젝트 구조 설명

- **BakjeServer** : 서버 프로젝트. Http 서버로서 작동하며 로컬에서 실행중인 MySQL과 직접 통신을 한다.
- **BakjeClient** : 클라이언트 프로젝트. Android에서 구동할 것을 전제로 하였다. Xamarin을 비롯하여 Android SDK/NDK 등을 세팅해야 한다. (아무것도 설치되지 않은 상태에서는 Visual Studio 2017의 설치 관리자를 사용하면 간단함)
- **BakjeProtocol** : 서버-클라이언트간 프로토콜을 정의한 코드.
- **DBScheme** : MySQL 스키마 백업


