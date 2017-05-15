using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BakjeProtocol
{
	/// <summary>
	/// 프로토콜에서 주고받는 최상위 데이터 포맷. 플레인 텍스트(or json) 하나와 그외 optional한 바이너리 데이터 여럿으로 구성.
	/// </summary>
    public class Packet
    {
		// Members

		string			m_plainText;				// 일반 텍스트 or JSON
		List<byte[]>	m_binaryDataList;			// 추가 바이너리 데이터

		public int binaryDataCount
		{
			get { return m_binaryDataList.Count; }
		}


		public Packet()
		{
			m_plainText			= "";
			m_binaryDataList	= new List<byte[]>();
		}

		/// <summary>
		/// 일반 텍스트 설정
		/// </summary>
		/// <param name="text"></param>
		public void SetPlainText(string text)
		{
			m_plainText	= text;
		}

		/// <summary>
		/// 오브젝트를 JSON으로 변환하여 플레인 텍스트로 설정한다.
		/// </summary>
		/// <param name="obj"></param>
		public void SetJSON(object obj)
		{
			var json	= new JavaScriptSerializer();
			m_plainText	= json.Serialize(obj);
		}

		public string GetPlainText()
		{
			return m_plainText;
		}

		public T GetJSONData<T>()
		{
			var json	= new JavaScriptSerializer();
			return json.Deserialize<T>(m_plainText);
		}

		/// <summary>
		/// 바이너리 데이터추가
		/// </summary>
		/// <param name="data"></param>
		public void AddBinaryData(byte[] data)
		{
			m_binaryDataList.Add(data);
		}

		/// <summary>
		/// 현재 등록된 바이너리 데이터 가져오기
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public byte[] GetBinaryData(int index)
		{
			return m_binaryDataList[index];
		}


		static byte[] SerializeUInt32(UInt32 integer)
		{
			var buffer	= BitConverter.GetBytes(integer);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(buffer);
			return buffer;
		}

		static UInt32 DeserializeUInt32(byte[] data)
		{
			var buffer	= data;
			if (BitConverter.IsLittleEndian)
			{
				buffer	= new byte[data.Length];
				data.CopyTo(buffer, 0);
				Array.Reverse(buffer);
			}
			return BitConverter.ToUInt32(buffer, 0);
		}

		static UInt32 DeserializeUInt32(byte[] data, int byteIndex)
		{
			var uint32Length	= sizeof(UInt32);
			var buffer			= new byte[uint32Length];
			Buffer.BlockCopy(data, byteIndex, buffer, 0, uint32Length);

			if (BitConverter.IsLittleEndian)
				Array.Reverse(buffer);

			return BitConverter.ToUInt32(buffer, 0);
		}

		/// <summary>
		/// 패킷을 만든다
		/// </summary>
		/// <returns></returns>
		public byte[] Pack()
		{
			var binaryDataCount	= this.binaryDataCount;
			var totalLength		= 0;		// 총 데이터 길이

			// 헤더 : 데이터 갯수 (uint32), 데이터 갯수만큼 각 데이터 길이 (uint32) 따라서 데이터 갯수 + 1만큼 uint32가 이어진 시퀀스
			var buf_dataCount	= SerializeUInt32((uint)(1 + binaryDataCount));
			totalLength			+= buf_dataCount.Length;	// 데이터 필드 길이

			// 텍스트 필드
			var textBuffer		= Encoding.UTF8.GetBytes(m_plainText);
			var buf_textSize	= SerializeUInt32((uint)textBuffer.Length);
			totalLength			+= buf_textSize.Length;		// 데이터 필드 길이
			totalLength			+= textBuffer.Length;		// 텍스트 데이터 길이
			
			// 추가 데이터 필드
			var buf_binSizes	= new byte[binaryDataCount][];
			for (var i = 0; i < binaryDataCount; i++)
			{
				var binSize		= m_binaryDataList[i].Length;
				var bufBinSize	= SerializeUInt32((uint)binSize);
				buf_binSizes[i]	= bufBinSize;
				totalLength		+= bufBinSize.Length;		// 데이터 필드 길이
				totalLength		+= binSize;					// 바이너리 데이터 길이
			}


			// 데이터 복사

			var packed	= new byte[totalLength];			// 전체 데이터 크기만큼 배열 생성
			var pointer	= 0;
			buf_dataCount.CopyTo(packed, pointer);			// 데이터 갯수 기록
			pointer		+= buf_dataCount.Length;

			buf_textSize.CopyTo(packed, pointer);			// 텍스트 길이 기록
			pointer		+= buf_textSize.Length;

			for (var i = 0; i < binaryDataCount; i++)		// 바이너리 데이터 길이 기록
			{
				var bufBinSize	= buf_binSizes[i];
				bufBinSize.CopyTo(packed, pointer);
				pointer	+= bufBinSize.Length;
			}

			textBuffer.CopyTo(packed, pointer);				// 실제 텍스트 데이터 기록
			pointer		+= textBuffer.Length;

			for (var i = 0; i < binaryDataCount; i++)		// 실제 바이너리 데이터 기록
			{
				var binData		= m_binaryDataList[i];
				binData.CopyTo(packed, pointer);
				pointer			+= binData.Length;
			}


			return packed;
		}

		/// <summary>
		/// 패킷 데이터에서 실제 데이터를 뽑아낸다.
		/// </summary>
		/// <param name="packed"></param>
		/// <returns></returns>
		public static Packet Unpack(byte[] packed)
		{
			var newPacket		= new Packet();

			var pointer			= 0;
			var uint32Length	= sizeof(UInt32);

			var dataCount		= DeserializeUInt32(packed, pointer);
			pointer				+= uint32Length;

			var textSize		= DeserializeUInt32(packed, pointer);
			pointer				+= uint32Length;
			
			var binaryCount			= dataCount - 1;
			var binaryDataSize	= new UInt32[binaryCount];
			for (var i = 0; i < binaryCount; i++)
			{
				binaryDataSize[i]	= DeserializeUInt32(packed, pointer);
				pointer				+= uint32Length;
			}

			var textBuffer		= new byte[textSize];
			Buffer.BlockCopy(packed, pointer, textBuffer, 0, (int)textSize);
			newPacket.m_plainText	= Encoding.UTF8.GetString(textBuffer);
			pointer				+= (int)textSize;

			for (var i = 0; i < binaryCount; i++)
			{
				var binSize		= binaryDataSize[i];
				var binaryData	= new byte[binSize];
				Buffer.BlockCopy(packed, pointer, binaryData, 0, (int)binSize);
				newPacket.m_binaryDataList.Add(binaryData);
				pointer			+= (int)binSize;
			}


			return newPacket;
		}
    }
}
