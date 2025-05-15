# 后端
[Unit]
Description=qmtdlt_tools service

[Service]
WorkingDirectory=/home/qmtdlt_tools/api/
ExecStart=dotnet /home/qmtdlt_tools/api/QmtdltTools.dll --urls http://*:5083 --contentRoot /home/qmtdlt_tools/api
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=qmtdlt_tools_api
User=root
Environment=ASPNETCORE_ENVIRONMENT=Development
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target

# Ubuntu mysql8 设置大小写不敏感
sudo cp /etc/mysql/mysql.conf.d/mysqld.cnf /home/mysqld.cnf.bakup

sudo apt purge mysql-server mysql-client mysql-common
sudo rm -rf /etc/mysql /var/lib/mysql /var/log/mysql
sudo apt autoremove
sudo apt autoclean

// 创建目录及文件 /etc/mysql/mysql.conf.d/mysqld.cnf
sudo vim /etc/mysql/mysql.conf.d/mysqld.cnf
lower_case_table_names=1
bind-address=0.0.0.0

sudo apt install mysql-server


sudo mysql_secure_installation
提示设置 root 密码时，输入 "12000asd"。
其他选项建议如下（可根据需要调整）：
删除匿名用户？是
禁止 root 远程登录？否（因为您需要远程访问）
删除测试数据库？是
立即刷新权限表？是

sudo mysql -u root -p
CREATE USER 'root'@'%' IDENTIFIED BY '12000asd';
GRANT ALL PRIVILEGES ON *.* TO 'root'@'%' WITH GRANT OPTION;
FLUSH PRIVILEGES;


sudo systemctl restart mysql
# 安装docker
sudo apt-get update
sudo apt-get upgrade
sudo apt-get install ca-certificates curl gnupg lsb-release
curl -fsSL http://mirrors.aliyun.com/docker-ce/linux/ubuntu/gpg | sudo apt-key add -
sudo sh -c 'echo "deb [arch=amd64] http://mirrors.aliyun.com/docker-ce/linux/ubuntu $(lsb_release -cs) stable" > /etc/apt/sources.list.d/docker.list'
sudo apt-get update
sudo apt-get install docker-ce docker-ce-cli containerd.io

# 验证是否成功安装了docker
sudo systemctl status docker
docker --version
vim /etc/docker/daemon.json
# daemon.json内容如下：
{
    "registry-mirrors": [
        "https://dockerproxy.com",
        "https://docker.m.daocloud.io",
        "https://cr.console.aliyun.com",
        "https://ccr.ccs.tencentyun.com",
        "https://hub-mirror.c.163.com",
        "https://mirror.baidubce.com",
        "https://docker.nju.edu.cn",
        "https://docker.mirrors.sjtug.sjtu.edu.cn",
        "https://github.com/ustclug/mirrorrequest",
        "https://registry.docker-cn.com"
    ]
}

# 重载配置文件，并重启 docker
sudo systemctl daemon-reload
sudo systemctl restart docker

# 查看 Registry Mirrors 配置是否成功
sudo docker info 


# 前端nginx
nginx.conf
```
user  nginx;
worker_processes  auto;

error_log  /var/log/nginx/error.log notice;
pid        /var/run/nginx.pid;

events {
    worker_connections  1024;
}

http {
    include       /etc/nginx/mime.types;
    default_type  application/octet-stream;

    log_format  main  '$remote_addr - $remote_user [$time_local] "$request" '
                      '$status $body_bytes_sent "$http_referer" '
                      '"$http_user_agent" "$http_x_forwarded_for"';

    access_log  /var/log/nginx/access.log  main;

    sendfile        on;
    keepalive_timeout  65;

    # Define the upstream backend
    upstream backend {
        server 47.95.17.189:5083; # Replace with your actual backend service IP and port
    }

    # Server block
    server {
        listen 80;

        location / {
            root   /usr/share/nginx/html/dist;
            index  index.html index.htm;
        }

        location /signalr-hubs/ {
            proxy_pass http://backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_cache_bypass $http_upgrade;
        }
    }

    # Include additional configuration files
    include /etc/nginx/conf.d/*.conf;
}
```

# docker  命令 
docker run -d \
  --name qmtdlt_web \
  -p 80:80 \
  -v /home/qmtdlt_tools/:/usr/share/nginx/html \
  -v ngconf:/etc/nginx \
  --restart always \
  nginx:latest
  
# 安装 redis
sudo apt install redis-server
sudo systemctl status redis-server
