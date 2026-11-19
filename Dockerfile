
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["YaaraFramesGifts/YaaraFramesGifts.csproj", "YaaraFramesGifts/"]
COPY ["YaaraFramesGifts.Shared/YaaraFramesGifts.Shared.csproj", "YaaraFramesGifts.Shared/"]

RUN dotnet restore "YaaraFramesGifts/YaaraFramesGifts.csproj"

COPY . .

WORKDIR /src/YaaraFramesGifts
RUN dotnet publish "YaaraFramesGifts.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

ENTRYPOINT ["dotnet", "YaaraFramesGifts.dll"]
