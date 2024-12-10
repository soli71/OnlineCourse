@echo off

echo Enter the version (e.g., v1.0.3-alpha):
set /p version=

echo Building Docker image...
docker build . --tag onlinecourse:%version%
docker build . --tag onlinecourse:latest

echo Tagging Docker image...
docker tag onlinecourse salmanaghaei/onlinecourse:%version%

echo Pushing Docker image to Docker Hub...
docker push salmanaghaei/onlinecourse:%version%

echo Done!
pause