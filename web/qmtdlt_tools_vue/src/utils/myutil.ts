
const isMobbile = () => {
    let flat = navigator.userAgent.toLowerCase()
    let isMobile = flat.match(/(iphone|ipod|ipad|android|blackberry|iemobile|opera mini|windows phone)/)
    return isMobile !== null
}

export { isMobbile }