# CfdiSATRecovery

Intento de autenticacion en los endpoints del SAT para la recuperacion de CFDI

Convertir FIEL archivo.cer y archivo.key a archivo.pfx con OpenSSL

```sh
pkcs8 -in <ruta del archivo key>.key -inform DER -out <ruta del archive de salida>.pem
x509 -in <ruta archivo certificado>.cer -inform DER -out <ruta archivo destino>.pem
pkcs12 -export -inkey <ruta archivo keypem>.pem -in <ruta archivo cerpem>.pem -out <ruta archivo final>.pfx
```
