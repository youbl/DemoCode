# DemoCode
收集整理一些最佳实践代码或测试代码  
  
Library目录：  
1、Beinet.Core：一些通用类库整理；  
2、Beinet.Request：对HttpWebRequest的扩展；  
此dll仅需要复制到web项目的bin目录下即可完成，对原业务代码无需任何调整；  
可以调用DemoCodeWeb/a.aspx页面进行测试（复制dll前后结果对比）  
3、Beinet.Feign：仿java的feign实现的Http api请求库；  
Feign是Java里的一个声明式的http api请求库，可以通过注解（类似.Net的特性）来快速并优雅的封装对http的调用，并且方便理解和后续的维护，已经广泛的在Spring Cloud的解决方案中应用。  
具体调用，可以参考 src\Test\Beinet.FeignDemoConsole 项目，里面有完整示例代码。    
或者参考我的博客：https://blog.csdn.net/youbl/article/details/105006665    
4、Beinet.EnumList: 扫描整个项目的枚举清单，并添加一个url路由 /actuator/enums, 以便快速浏览项目中的枚举；  
便于自己或其它依赖此枚举的项目，可以快速发现枚举列表的变更情况，比如前端.  
对项目代码无侵入，编译后，直接把这个dll复制到对方站点bin目录下即可。  
注：此项目依赖NewtonSoft  
  
  
其它演示：  
1、src\DemoCodeConsole.MQBaseDemo.MQRun:  
基于生产消费者模式的Demo演示代码。  

2、src\DemoForDynamicProxy：  
演示如何只定义接口，不编写实现，也能创建接口实例的过程。  
使用场景：一些业务项目只需要定义接口，然后由框架进行统一实现的场景；  
举例1：类似于Java的Feign，业务只需要定义接口和Attribute声明，框架层统一完成http请求。  
举例2：类似于Java的JPA，业务只需要定义仓储层接口，框架层统一完成数据库操作  
  
3、src\Test\Beinet.CronDemoConsole：  
基于Cron表达式的计划任务控制实现和演示.  
简介：  
```
类似于Linux的Crontab调度语法，也类似于Java SpringBoot时的注解 @Scheduled(cron = "0 0 4 * * *")，
但是Linux的Crontab最小单位是分钟，Java和本Demo的最小单位是秒。
下面是本Demo的Cron表达式介绍：
表达式由6个或7个由空格分隔的字符串组成，如：
* * * * * * *
分别表示：秒 分 时 日 月 周 年：
说明1：最右边的年，是可选的，范围是1970~2099；
说明2：周的范围为0~7，0和7都是表示星期日，1表示星期一，如此类推；
说明3：每个字符串的定义是一致的，大致有如下几种格式(用秒演示)：
* 每秒运行一次；
*/8 每8秒运行一次；
1,2,3 第1秒、第2秒和第3秒各运行一次；
3-10 第3秒到第10秒，每秒运行一次，共8次；
20-30/3 从第20秒到第30秒，每3秒运行一次，即20秒、23秒、26秒、29秒，共4次；
说明4：组合定义举例：
1,6 58 22 1 * * 每月1号的22点58分1秒 和 6秒，各运行1次；
0 0 9 1 * 0,6 每月1号9点整，如果是周末，执行1次；
0 0 9 1 * * 2022 2022年每月1号9点整，执行，全年共12次。 
```