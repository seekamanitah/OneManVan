# Copy and paste this entire block into your SSH session
# It will fix the login page issue on your server

cd /opt/onemanvan || { echo "Error: /opt/onemanvan not found"; exit 1; }

echo "Backing up files..."
cp OneManVan.Web/Components/App.razor OneManVan.Web/Components/App.razor.bak
cp OneManVan.Web/Components/Routes.razor OneManVan.Web/Components/Routes.razor.bak

echo "Fixing App.razor..."
cat > OneManVan.Web/Components/App.razor << 'EOF'
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <ResourcePreloader />
    <link rel="stylesheet" href="@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]" />
    <link rel="stylesheet" href="@Assets["app.css"]" />
    <link rel="stylesheet" href="@Assets["OneManVan.Web.styles.css"]" />
    <ImportMap />
    <link rel="icon" type="image/png" href="favicon.png" />
    <HeadOutlet />
</head>

<body>
    <Routes @rendermode="InteractiveServer" />
    <script id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">×</a>
    </script>
    <script src="@Assets["_framework/blazor.web.js"]"></script>
    <script src="@Assets["Components/Account/Shared/PasskeySubmit.razor.js"]" type="module"></script>
</body>

</html>
EOF

echo "Fixing Routes.razor..."
cat > OneManVan.Web/Components/Routes.razor << 'EOF'
@using OneManVan.Web.Components.Account.Shared
@using Microsoft.AspNetCore.Components.Web

<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
            <NotAuthorized>
                @if (routeData.PageType.Namespace?.Contains("Account") == true)
                {
                    @* Render Account pages without InteractiveServer mode to avoid auth loops *@
                    <CascadingValue Value="@((IComponentRenderMode?)null)" Name="RendererInfo">
                        <RouteView RouteData="routeData" />
                    </CascadingValue>
                }
                else
                {
                    <RedirectToLogin />
                }
            </NotAuthorized>
            <Authorizing>
                <div class="d-flex justify-content-center align-items-center" style="height: 100vh;">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </div>
            </Authorizing>
        </AuthorizeRouteView>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="typeof(Layout.MainLayout)">
            <div class="container mt-5">
                <div class="alert alert-warning">
                    <h4>Not Found</h4>
                    <p>Sorry, the content you are looking for does not exist.</p>
                    <a href="/" class="btn btn-primary">Go to Home</a>
                </div>
            </div>
        </LayoutView>
    </NotFound>
</Router>
EOF

echo ""
echo "Files fixed! Rebuilding containers..."
docker compose -f docker-compose-full.yml down
docker rmi $(docker images -q 'tradeflow-webui' 2>/dev/null) 2>/dev/null || true
docker compose -f docker-compose-full.yml build --no-cache webui
docker compose -f docker-compose-full.yml up -d

echo ""
echo "Waiting 30 seconds for startup..."
sleep 30

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
docker ps --format "table {{.Names}}\t{{.Status}}"
echo ""
echo "Test at: http://192.168.100.107:5000"
