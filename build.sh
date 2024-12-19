#!/bin/sh
# Hướng dẫn cài đặt dotnet 6.0
# sudo add-apt-repository ppa:dotnet/backports
# apt install software-properties-common
# wget https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
# sudo dpkg -i packages-microsoft-prod.deb
# rm packages-microsoft-prod.deb
# apt update; apt install dotnet-sdk-6.0

# Hướng dẫn cài đặt docker compose
# apt install ca-certificates curl
# curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
# chmod a+r /etc/apt/keyrings/docker.asc
#echo \
#  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
#  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
#  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
# apt update; apt install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin -y;
# service docker start;

dotnet restore;
dotnet msbuild -property:Configuration=Release /t:Clean ArtworkCore.sln;
dotnet msbuild -property:Configuration=Release ArtworkCore.sln;

docker rm -f $(docker ps -a | grep 'artwork_core'); docker rmi $(docker images | grep 'artwork_core');


cd ./artwork-core/bin/Release/net6.0 && docker build -f ../../../Dockerfile -t artwork-core .;
cd ../../../../;


docker compose -f docker_build/artwork_core.yml up -d;

docker update --restart unless-stopped artwork_core && docker start artwork_core;

cd ../;
