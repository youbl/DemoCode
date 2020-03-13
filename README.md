# DemoCode
收集整理一些最佳实践代码或测试代码  
  
Library目录：  
1、Beinet.Core：一些通用类库整理；  
2、Beinet.Request：对HttpWebRequest的扩展；  
此dll仅需要复制到web项目的bin目录下即可完成，对原业务代码无需任何调整；  
可以调用DemoCodeWeb/a.aspx页面进行测试（复制dll前后结果对比）  
  
  
1、DemoCodeConsole.MQBaseDemo.MQRun:  
基于生产消费者模式的Demo演示代码。  

2、DemoForDynamicProxy：  
演示如何只定义接口，不编写实现，也能创建接口实例的过程。  
使用场景：一些业务项目只需要定义接口，然后由框架进行统一实现的场景；  
举例1：类似于Java的Feign，业务只需要定义接口和Attribute声明，框架层统一完成http请求。  
举例2：类似于Java的JPA，业务只需要定义仓储层接口，框架层统一完成数据库操作  
  
