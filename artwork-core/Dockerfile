FROM mcr.microsoft.com/dotnet/aspnet:6.0
ENV TZ="Asia/Ho_Chi_Minh"
WORKDIR /app
COPY . /app
ENV ASPNETCORE_URLS=http://*:80
ENTRYPOINT ["dotnet", "ArtworkCore.dll"]
