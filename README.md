# Edge Cases & Prevention

**1. Risk)** Random numara üreten API'nin manipülasyonu.

  - Sorun : Kullanıcı, verilen random number API'yi kendisi çağırabilir ya da API'den dönen cevapı bulup yanıtı değiştirebilir. 
		    Örneğin kullanıcı 7 gelmesi gereken indexi 4 yaparak daha büyük ödül kazanmasını sağlayabilir.
            Yaptığım uygulamada API'deki indexe, istemci yani oyuncunun oynadığı oyun karar veriyor. (İstemcinin getirdiği sonuca güveniliyor)
	
   - Öneri : Cloud Function (*Server-Otoriter*) uygulanarak index sonucunu sunucu üretmelidir. 
			      Örnek senaryoda kullanıcı spin ister (çarkı döndürür), sunucu random number API'yi kullanarak rastgele sonucu üretir, ödülü ve cooldown'ı database'e yazar ve sonucu istemciyle paylaşır.
			  
**2. Risk)** Bekleme süresinin manipülasyonu.

  - Sorun : Kullanıcı, spinler arasındaki bekleme süresinin atlanması için cihaz saatini değiştirebilir.
            Benim uygulamamda da bu açık vardır. Örneğin 15 dakika olan bekleme süresini aşmak için cihazın saati 15 dakika ileri alındığında cooldown'ı devre dışı bırakarak daha çok spin yapabilmektedir. Çünkü cooldown süresini, kullanıcının cihazının mevcut saatine 			    göre veritabanına yazdım.
  - Öneri : Bir üst riskin önerideki gibi spin akışını istemci tarafı değil sunucu tarafı yapmalı. 
	          Cooldown yazma işleminin de *Cloud Function*'a taşınarak *serverTimestamp* ile yapılması gerekmektedir. Böylece istemcinin yani kullanıcının saatinin bir önemi kalmayacaktır. 
			      Ayrıca Firestore rules içerisinde istemcinin yazma yetkisinin olmaması gerekir. Yazma yetkisinin sadece sunucuda olmasıyla güvenlik önlemi arttırılabilir.
			  
**3. Risk)** Aynı spin için birden çok ödül alma manipülasyonu.

  - Sorun : Kullanıcı, spin sonucunu gördüğü esnada sonuç veritabanına yazılmadan oyunu kapatırsa ya da interneti keserse ve tekrar oyuna girerek aynı spinle daha büyük ödül alabilir
			      Uygulamamda bu açığı büyük oranda kapattığımı düşünüyorum. Kullanıcı her spin işlemini yaptığında benzersiz bir spinId oluşturarak veritabanında sakladığım ledger koleksiyonuna ekledim.
            Yeni bir spin işleminde aynı spinId ledger koleksiyonumda varsa ödülü dağıtmadan ve veritabanına yazmadan işlemi sonlandırıyorum. Böylece ikinci kez ödül verilmiyor. Oyun tekrar açıldığında Spin butonu interactable değil, buton interactable olup yeni bir spin               işleminde yeni bir benzersiz spinId oluşacağı için ikinci kez ödül verilmemektedir. (*Idempotency*)
    SpinId veritabanına yazılmadan uygulama kapatılırsa/internet kesilirse veya başka bir açık oluştuğu durumda; Spin butonuna basıldığı anda direkt olarak benzersiz bir spinId üretiliyor. Üretilen spinId PlayerPrefs                     içerisinde saklanıyor. Ödül veritabanına yazılmadan uygulama kapatılıp açılırsa PlayerPrefs içerisinde bir spinId olduğu için ilgili spinId ile veritabanına ödüller 0 olacak şekilde veritabanına yazılıyor ve cooldown devreye giriyor. (Veritabanına yazma işleminden sonra PlayerPrefs temizleniyor.)  
  - Öneri :     

**4. Risk)** Kullanıcının spin sırasında bağlantıyı kesme durumu.

- Sorun :
