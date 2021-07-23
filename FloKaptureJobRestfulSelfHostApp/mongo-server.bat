CD/
C:
CD  "C:\Program Files\MongoDB\Server\4.4\bin\"
mongod.exe --port 28000 --bind_ip 127.0.0.1 --auth --dbpath D:\DotNetCore-Projects\DotNetCoreMongoDb --directoryperdb --tlsMode requireTLS --tlsCertificateKeyFile "D:\DotNetCore-Projects\FloKaptureJobProcessingApp\FloKaptureJobRestfulSelfHostApp\certificates\test-server.pem" --tlsCAFile "D:\DotNetCore-Projects\FloKaptureJobProcessingApp\FloKaptureJobRestfulSelfHostApp\certificates\test-ca.pem" --tlsAllowInvalidHostnames --tlsAllowInvalidCertificates

## PS C:\Program Files\MongoDB\Server\4.4\bin> .\mongo.exe --port 29000 -u admin -p Admin@123 --host 127.0.0.1 --tls --tlsCertificateKeyFile "C:\Users\Yogesh Sonawane\mongodb-client\test-client.pem" --tlsCAFile "C:\Users\Yogesh Sonawane\mongodb-certificates\test-ca.pem" --tlsAllowInvalidHostnames --tlsAllowInvalidCertificates