# DemoCode
收集整理一些最佳实践代码或测试代码  
  
Library目录：  
1、Beinet.Core：一些通用类库整理；  
2、Beinet.Request：对HttpWebRequest的扩展；  
此dll仅需要复制到web项目的bin目录下即可完成，对原业务代码无需任何调整；  
可以调用DemoCodeWeb/a.aspx页面进行测试（复制dll前后结果对比）  
3、Beinet.Feign：仿java的feign实现的Http api请求库；  
Feign是Java里的一个声明式的http api请求库，可以通过注解（类似.Net的特性）来快速并优雅的封装对http的调用，并且方便理解和后续的维护，已经广泛的在Spring Cloud的解决方案中应用。  
具体调用，可以参考 Test\Beinet.FeignDemoConsole 项目，里面有完整示例代码。    
  
  
1、DemoCodeConsole.MQBaseDemo.MQRun:  
基于生产消费者模式的Demo演示代码。  

2、DemoForDynamicProxy：  
演示如何只定义接口，不编写实现，也能创建接口实例的过程。  
使用场景：一些业务项目只需要定义接口，然后由框架进行统一实现的场景；  
举例1：类似于Java的Feign，业务只需要定义接口和Attribute声明，框架层统一完成http请求。  
举例2：类似于Java的JPA，业务只需要定义仓储层接口，框架层统一完成数据库操作  
  
