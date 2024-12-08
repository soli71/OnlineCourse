@echo off
echo Building Docker image...
docker build . --tag morteza:v1.0.1-alpha
docker build . --tag morteza:latest

echo Tagging Docker image...
docker tag morteza salmanaghaei/morteza:v1.0.1-alpha

echo Pushing Docker image to Docker Hub...
docker push salmanaghaei/morteza:v1.0.1-alpha

echo Done!
pause