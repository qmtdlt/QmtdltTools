import FingerprintJS from '@fingerprintjs/fingerprintjs'


const getVisitorId = async () => {
  const fp = await FingerprintJS.load()
  const result = await fp.get()
  return result.visitorId
}

const getOrCreateGuestId = () =>{
    let guestId = localStorage.getItem("guest_id");
    if (!guestId) {
        guestId = crypto.randomUUID(); // 生成唯一ID
        localStorage.setItem("guest_id", guestId);
        console.log("新游客 ID 已创建：", guestId);
    } else {
        console.log("已有游客 ID：", guestId);
    }
    return guestId;
}

export { getOrCreateGuestId,getVisitorId };