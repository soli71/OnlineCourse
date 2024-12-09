@echo off
echo Building Docker image...
docker build . --tag onlinecourse:v1.0.1-alpha
docker build . --tag onlinecourse:latest

echo Tagging Docker image...
docker tag onlinecourse salmanaghaei/onlinecourse:v1.0.1-alpha

echo Pushing Docker image to Docker Hub...
docker push salmanaghaei/onlinecourse:v1.0.1-alpha

echo Done!
pause