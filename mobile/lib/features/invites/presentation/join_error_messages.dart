/// Katılım hata kodlarının kullanıcıya gösterilen mesajları (spec 0006).
const joinErrorMessages = <String, String>{
  'INVITE_INVALID': 'Bu davet kodu geçersiz. Kodu kontrol edip tekrar dene.',
  'INVITE_EXPIRED':
      'Bu davet kodunun süresi dolmuş. Yöneticiden yeni kod iste.',
  'INVITE_REVOKED': 'Bu davet kodu iptal edilmiş. Yöneticiden yeni kod iste.',
  'CAPACITY_EXCEEDED': 'Bu grup dolu, yeni üye kabul edemiyor.',
  'ALREADY_MEMBER': 'Zaten bu grubun üyesisiniz.',
  'ALREADY_IN_GROUP': 'Zaten başka bir grubun üyesisin. Yeni gruba katılmak için önce ondan ayrılmalısın.',
};
