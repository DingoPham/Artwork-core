#Stage 1
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS build
WORKDIR /source

#Stage 2
COPY *.csproj .
RUN dotnet restore

#Stage 3
COPY . .
RUN dotnet publish -c Release -o /app

#Stage 4
FROM mcr.microsoft.com/dotnet/aspnet:6.0
ENV TZ="Asia/Ho_Chi_Minh"
WORKDIR /app

#Stage 5
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

#Stage 6
ENTRYPOINT ["dotnet", "ArtworkCore.dll"]
